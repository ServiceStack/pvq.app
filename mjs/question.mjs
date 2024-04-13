import { ref, computed, watch, watchEffect, nextTick, onMounted, onUpdated, getCurrentInstance  } from "vue"
import { $$, $1, on, JsonServiceClient, EventBus, toDate, humanize, leftPart, lastRightPart } from "@servicestack/client"
import { useClient, useAuth, useUtils } from "@servicestack/vue"
import { mount, alreadyMounted, forceMount } from "app.mjs"
import { addCopyButtonToCodeBlocks } from "./header.mjs"
import {
    UserPostData, PostVote, GetQuestionFile,
    AnswerQuestion, UpdateQuestion, PreviewMarkdown, GetAnswerBody, CreateComment, GetMeta,
    DeleteQuestion, DeleteComment, GetUserReputations, CommentVote,
    ShareContent, FlagContent, GetAnswer,
} from "dtos.mjs"

const client = new JsonServiceClient()
const pageBus = new EventBus()
let meta = null
let userReputations = {}

function getComments(id) {
    if (!meta) return []
    return meta.comments && meta.comments[id] || []
}
const signInUrl = () => `/Account/Login?ReturnUrl=${location.pathname}`

function getContentType(refId) {
    return refId.indexOf('-') < 0
        ? 'Question'
        : parseInt(lastRightPart(refId, '-')) ? 'Comment' : 'Answer'
}

function formatDate(date) {
    const d = toDate(date)
    return d.toLocaleString('en-US', { month: 'short' }) + ' ' + d.getDate() + ' at '
        + `${d.getHours()}`.padStart(2,'0')+ `:${d.getMinutes()}`.padStart(2,'0')
}

function applyGlobalChanges() {
    globalThis?.hljs?.highlightAll()
    addCopyButtonToCodeBlocks()
}

const svgPaths = {
    up: {
        empty: '<path fill="currentColor" d="M3 19h18a1.002 1.002 0 0 0 .823-1.569l-9-13c-.373-.539-1.271-.539-1.645 0l-9 13A.999.999 0 0 0 3 19m9-12.243L19.092 17H4.908z"/>',
        solid: '<path fill="currentColor" d="M3 19h18a1.002 1.002 0 0 0 .823-1.569l-9-13c-.373-.539-1.271-.539-1.645 0l-9 13A.999.999 0 0 0 3 19"/>',
    },
    down: {
        empty: '<path fill="currentColor" d="M21.886 5.536A1.002 1.002 0 0 0 21 5H3a1.002 1.002 0 0 0-.822 1.569l9 13a.998.998 0 0 0 1.644 0l9-13a.998.998 0 0 0 .064-1.033M12 17.243L4.908 7h14.184z"/>',
        solid: '<path fill="currentColor" d="M11.178 19.569a.998.998 0 0 0 1.644 0l9-13A.999.999 0 0 0 21 5H3a1.002 1.002 0 0 0-.822 1.569z"/>',
    }
}

globalThis.removeComment = async function (el) {
    const parentEl = el.parentElement, 
          id = parentEl.dataset.id,
          created = parseInt(parentEl.dataset.created),
          createdBy = parentEl.dataset.createdby
    if (confirm('Are you sure?')) {
        const api = await client.apiVoid(new DeleteComment({
            id,
            created,
            createdBy,
        }))
        if (api.succeeded) {
            parentEl.parentElement.removeChild(parentEl)
        } else {
            alert(api.errorMessage)
        }
    }
}

let userPostVotes = {upVoteIds:[], downVoteIds:[]}
let origPostValues = {upVoteIds:[], downVoteIds:[]}
function getValue(postVotes, refId) {
    return (postVotes.upVoteIds.includes(refId) ? 1 : postVotes.downVoteIds.includes(refId) ? -1 : 0)
}
function setValue(refId, value) {
    userPostVotes.upVoteIds = userPostVotes.upVoteIds.filter(x => x !== refId)
    userPostVotes.downVoteIds = userPostVotes.downVoteIds.filter(x => x !== refId)
    if (value === 1) {
        userPostVotes.upVoteIds.push(refId)
    }
    if (value === -1) {
        userPostVotes.downVoteIds.push(refId)
    }
}

async function updateUserData(postId) {
    const api = await client.api(new UserPostData({ postId }))
    if (api.succeeded) {
        origPostValues = api.response
        userPostVotes = Object.assign({}, origPostValues)
        pageBus.publish('userPostData:load')
    }
}

function toHumanReadable(n) {
    if (n >= 1_000_000_000)
        return (n / 1_000_000_000).toFixed(1) + "b";
    if (n >= 1_000_000)
        return (n / 1_000_000).toFixed(1) + "m";
    if (n >= 1_000)
        return (n / 1_000).toFixed(1) + "k";
    return n.toLocaleString();
}

async function loadVoting(ctx) {
    const { client, postId, userName, user, hasRole } = ctx

    function updateVote(el) {
        const up = el.querySelector('.up')
        const down = el.querySelector('.down')
        const score = el.querySelector('.score')

        const value = getValue(userPostVotes, el.dataset.refid)
        up.classList.toggle('text-green-600',value === 1)
        up.innerHTML = value === 1 ? svgPaths.up.solid : svgPaths.up.empty
        down.classList.toggle('text-green-600',value === -1)
        down.innerHTML = value === -1 ? svgPaths.down.solid : svgPaths.down.empty
        score.innerHTML = toHumanReadable(parseInt(score.dataset.score) + value - getValue(origPostValues, el.dataset.refid))
    }

    $$('.voting').forEach(el => {
        const refId = el.dataset.refid
        const createdBy = el.closest('[data-createdby]')?.dataset.createdby
        async function vote(value) {
            if (!userName) {
                location.href = `/Account/Login?ReturnUrl=${encodeURIComponent(location.pathname)}`
                return
            }
            if (userName === createdBy) 
                return

            const prevValue = getValue(userPostVotes, refId)
            setValue(refId, value)
            updateVote(el)

            const api = await client.apiVoid(new PostVote({ refId, up:value === 1, down:value === -1 }))
            if (!api.succeeded) {
                setValue(refId, prevValue)
                updateVote(el)
            } else {
                loadUserReputations(userName)
                setTimeout(() => loadUserReputations(userName), 5000)
            }
        }
        function disableSelf(svg) {
            if (userName === createdBy) {
                svg.classList.remove('hover:text-green-600','cursor-pointer')
                svg.classList.add('text-gray-400')
            }
            return svg
        }

        on(disableSelf(el.querySelector('.up')), {
            click(e) {
                vote(getValue(userPostVotes, refId) === 1 ? 0 : 1)
            }
        })
        on(disableSelf(el.querySelector('.down')), {
            click(e) {
                vote(getValue(userPostVotes, refId) === -1 ? 0 : -1)
            }
        })
    })

    await updateUserData(postId)
    $$('.voting').forEach(updateVote)
}

const ShareDialog = {
    template:`
  <div v-if="show" :class="[transition1,'absolute top-4 left-0 z-10 min-w-[350px] p-4 mt-2 w-56 origin-top-left rounded-md bg-white shadow-lg ring-1 ring-black ring-opacity-5 focus:outline-none']" role="menu" aria-orientation="vertical" aria-labelledby="menu-button" tabindex="-1">
    <div class="py-1" role="none">
        <div>
            <b>Share a link to this {{contentType}}</b>
            <button type="button" @click="show=false" title="Close" class="absolute top-2 right-2 bg-white dark:bg-black','rounded-md text-gray-400 hover:text-gray-500 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 dark:ring-offset-black">
              <span class="sr-only">Close</span>
              <svg class="h-4 w-4" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
            </button>            
        </div>
        <div class="py-4">
            <TextInput id="shareUrl" label="" :value="url" />
        </div>
        <div class="flex justify-between">
            <div :class="['flex items-center text-base text-gray-600 hover:text-gray-800', copying ? '' : 'cursor-pointer']" @click="copy">
                <svg v-if="!copying" class="w-6 h-6" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><g fill="none"><path d="M8 5H6a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2v-1M8 5a2 2 0 0 0 2 2h2a2 2 0 0 0 2-2M8 5a2 2 0 0 1 2-2h2a2 2 0 0 1 2 2m0 0h2a2 2 0 0 1 2 2v3m2 4H10m0 0l3-3m-3 3l3 3" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"></path></g></svg>
                <svg v-else class="w-6 h-6 text-green-500" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path></svg>
                <span class="ml-1">{{copying ? 'copied' : 'copy'}}</span>
            </div>
            <div class="flex mr-2">
                <svg class="w-6 h-6 cursor-pointer focus:outline-none hover:ring-2 hover:ring-offset-2 hover:ring-indigo-600 mr-2" @click="openUrl('x')" xmlns="http://www.w3.org/2000/svg" width="1em" height="1em" viewBox="0 0 256 256"><title>Share on twitter.com</title><g fill="none"><rect width="256" height="256" fill="#fff" rx="60"/><rect width="256" height="256" fill="#1d9bf0" rx="60"/><path fill="#fff" d="M199.572 91.411c.11 1.587.11 3.174.11 4.776c0 48.797-37.148 105.075-105.075 105.075v-.03A104.54 104.54 0 0 1 38 184.677c2.918.351 5.85.526 8.79.533a74.154 74.154 0 0 0 45.865-15.839a36.976 36.976 0 0 1-34.501-25.645a36.811 36.811 0 0 0 16.672-.636c-17.228-3.481-29.623-18.618-29.623-36.198v-.468a36.705 36.705 0 0 0 16.76 4.622c-16.226-10.845-21.228-32.432-11.43-49.31a104.814 104.814 0 0 0 76.111 38.582a36.95 36.95 0 0 1 10.683-35.283c14.874-13.982 38.267-13.265 52.249 1.601a74.105 74.105 0 0 0 23.451-8.965a37.061 37.061 0 0 1-16.234 20.424A73.446 73.446 0 0 0 218 72.282a75.023 75.023 0 0 1-18.428 19.13"/></g></svg>
                <svg class="w-6 h-6 cursor-pointer focus:outline-none hover:ring-2 hover:ring-offset-2 hover:ring-indigo-600 mr-2" @click="openUrl('t')" xmlns="http://www.w3.org/2000/svg" width="0.88em" height="1em" viewBox="0 0 448 512"><title>Share on threads.net</title><path fill="currentColor" d="M64 32C28.7 32 0 60.7 0 96v320c0 35.3 28.7 64 64 64h320c35.3 0 64-28.7 64-64V96c0-35.3-28.7-64-64-64zm230.2 212.3c19.5 9.3 33.7 23.5 41.2 40.9c10.4 24.3 11.4 63.9-20.2 95.4c-24.2 24.1-53.5 35-95.1 35.3h-.2c-46.8-.3-82.8-16.1-106.9-46.8c-21.5-27.3-32.6-65.4-33-113.1v-.2c.4-47.7 11.5-85.7 33-113.1c24.2-30.7 60.2-46.5 106.9-46.8h.2c46.9.3 83.3 16 108.2 46.6c12.3 15.1 21.3 33.3 27 54.4l-26.9 7.2c-4.7-17.2-11.9-31.9-21.4-43.6c-19.4-23.9-48.7-36.1-87-36.4c-38 .3-66.8 12.5-85.5 36.2c-17.5 22.3-26.6 54.4-26.9 95.5c.3 41.1 9.4 73.3 26.9 95.5c18.7 23.8 47.4 36 85.5 36.2c34.3-.3 56.9-8.4 75.8-27.3c21.5-21.5 21.1-47.9 14.2-64c-4-9.4-11.4-17.3-21.3-23.3c-2.4 18-7.9 32.2-16.5 43.2c-11.4 14.5-27.7 22.4-48.4 23.5c-15.7.9-30.8-2.9-42.6-10.7c-13.9-9.2-22-23.2-22.9-39.5c-1.7-32.2 23.8-55.3 63.5-57.6c14.1-.8 27.3-.2 39.5 1.9c-1.6-9.9-4.9-17.7-9.8-23.4c-6.7-7.8-17.1-11.8-30.8-11.9h-.4c-11 0-26 3.1-35.6 17.6l-23-15.8c12.8-19.4 33.6-30.1 58.5-30.1h.6c41.8.3 66.6 26.3 69.1 71.8c1.4.6 2.8 1.2 4.2 1.9zm-71.8 67.5c17-.9 36.4-7.6 39.7-48.8c-8.8-1.9-18.6-2.9-29-2.9c-3.2 0-6.4.1-9.6.3c-28.6 1.6-38.1 15.5-37.4 27.9c.9 16.7 19 24.5 36.4 23.6z"/></svg>
                <svg class="w-6 h-6 cursor-pointer focus:outline-none hover:ring-2 hover:ring-offset-2 hover:ring-indigo-600 mr-2" @click="openUrl('f')" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 128 128"><title>Share on facebook.com</title><rect width="118.35" height="118.35" x="4.83" y="4.83" fill="#3d5a98" rx="6.53" ry="6.53"/><path fill="#fff" d="M86.48 123.17V77.34h15.38l2.3-17.86H86.48v-11.4c0-5.17 1.44-8.7 8.85-8.7h9.46v-16A127 127 0 0 0 91 22.7c-13.62 0-23 8.3-23 23.61v13.17H52.62v17.86H68v45.83z"/></svg>
            </div>
        </div>
    </div>
  </div>
    `,
    emits:['done'],
    props: {
        refId:String,
    },
    setup(props, { emit }) {
        const { user } = useAuth()
        const { transition } = useUtils()
        const copying = ref(false)

        const show = ref(false)
        const transition1 = ref('transform opacity-100 scale-100')
        let timeout = null
        const rule1 = {
            entering: { cls: 'transition ease-out duration-100', from: 'transform opacity-0 scale-95', to: 'transform opacity-100 scale-100' },
            leaving:  { cls: 'transition ease-in duration-75', from: 'transform opacity-100 scale-100', to: 'transform opacity-0 scale-95' }
        }
        watch(show, () => {
            transition(rule1, transition1, show.value)
            if (!show.value) {
                clearTimeout(timeout)
                timeout = setTimeout(done, 700)
            }
        })
        show.value = true

        const contentType = computed(() => getContentType(props.refId))
        const url = computed(() => location.origin + '/q/' + props.refId + (user.value?.userName ? '/' + user.value.userId : ''))

        function done() {
            emit('done')
        }
        
        function copy() {
            navigator.clipboard.writeText(url.value)
            copying.value = true
            setTimeout(() => copying.value = false, 3000)
        }
        
        onMounted(() => {
            nextTick(() => $1('#shareUrl').select())
        })
        
        function openUrl(name) {
            const href = url.value.replace('localhost:5001','pvq.app')
            const to = name === 'f'
                ? `https://www.facebook.com/sharer/sharer.php?u=${encodeURIComponent(href)}2&ref=fbshare&t=${encodeURIComponent(document.title)}` 
                : name === 'x'
                    ? `https://twitter.com/share?url=${encodeURIComponent(href)}&ref=twitbtn&text=${encodeURIComponent(document.title)}`
                    : `https://threads.net/intent/post?text=${encodeURIComponent(document.title + '\n\n' + href)}`
            if (to) {
                window.open(to, '_blank', `toolbar=no,location=0,status=no,titlebar=no,menubar=no,width=650,height=550,scrollbars=yes`)
            }
        }
        
        return { show, transition1, url, copying, contentType, openUrl, copy, done }
    }
}

const ReportDialog = {
    template:`<ModalDialog class="z-30" sizeClass="sm:max-w-prose sm:w-full" @done="done">
        <form @submit.prevent="submit">
            <div class="shadow overflow-hidden sm:rounded-md bg-white dark:bg-black">
                <div class="relative px-4 py-5 sm:p-6">
                    <fieldset>
                        <legend class="text-base font-medium text-gray-900 dark:text-gray-100 text-center mb-4">Flag {{contentType}}</legend>  
                        <ErrorSummary except="type,description" />
                        <div class="grid grid-cols-6 gap-6">
                            <div class="col-span-6">
                            <fieldset>
                              <legend class="sr-only">Flag Issue</legend>
                              <div class="space-y-5">
                                <div v-for="x in ReportTypes" class="relative flex items-start">
                                  <div class="flex h-6 items-center">
                                    <input :id="x.value" :value="x.value" :aria-describedby="x.value + '-description'" name="type" v-model="request.type" type="radio" class="h-4 w-4 border-gray-300 text-indigo-600 focus:ring-indigo-600">
                                  </div>
                                  <div class="ml-3 text-sm leading-6">
                                    <label :for="x.value" class="font-medium text-gray-900 dark:text-gray-50">{{x.label}}</label>
                                    <p :id="x.value + '-description'" class="text-gray-500">{{x.text}}</p>
                                  </div>
                                </div>
                              </div>
                            </fieldset>
                            </div>
                            <div class="col-span-6">
                                <TextareaInput id="reason" v-model="request.reason" placeholder="" label="Reason (optional)" />
                            </div>
                        </div>
                    </fieldset>
                </div>
                <div class="mt-4 px-4 py-3 bg-gray-50 dark:bg-gray-900 text-right sm:px-6">
                    <div class="flex justify-between items-center">
                        <div>
                            <Loading v-if="loading" />
                        </div>
                        <div>
                            <SecondaryButton class="mr-2" @click="done">Cancel</SecondaryButton>
                            <PrimaryButton type="submit">Submit</PrimaryButton>
                        </div>
                    </div>
                </div>
            </div>
        </form>
    </ModalDialog>`,
    emits:['done'],
    props: {
        refId:String,
    },
    setup(props, { emit }) {
        const client = useClient()
        const loading = client.loading

        const contentType = computed(() => getContentType(props.refId))

        const request = ref(new FlagContent({
            refId: props.refId
        }))

        async function submit() {
            const api = await client.api(request.value)
            if (api.succeeded) {
                done()
            }
        }

        function done() {
            emit('done')
        }
        
        const ReportTypes = [
            {
                value: 'Spam',
                text: 'Poor response, promotes a product or service without disclosing affiliation',
            },
            {
                value: 'Offensive',
                text: 'Uses rude, abusive, disrespectful or offensive language',
            },
            {
                value: 'Duplicate',
                text: 'This has already been asked or answered before',
            },
            {
                value: 'NotRelevant',
                text: 'This is not relevant to the question or answer',
            },
            {
                value: 'LowQuality',
                text: `This is a low quality or low effort ${contentType.value.toLowerCase()}`,
            },
            {
                value: 'Plagiarized',
                text: `This ${contentType.value.toLowerCase()} is copied without attribution`,
            },
            {
                value: 'NeedsReview',
                text: 'Other issues, needs review by a moderator. Please provide specific details',
            }
        ]
        ReportTypes.forEach(x => x.label = humanize(x.value))

        return { request, loading, contentType, ReportTypes, submit, done }
    }
}

const QuestionDialogs = {
    components: { ReportDialog },
    template: `<div>
        <ReportDialog v-if="show==='ReportDialog'" :refId="refId" @done="done" />
    </div>`,
    props:['bus'],
    setup(props, { emit }) {
        
        const show = ref('')
        const refId = ref('')
        
        function done() {
            show.value = ''
            emit('done')
        }
        
        onMounted(() => {
            props.bus.subscribe('showReportDialog', id => {
                show.value = 'ReportDialog'
                refId.value = id
            })
            props.bus.subscribe('showShare', id => {
                show.value = 'Share'
                refId.value = id
            })
        })
        
        return { show, refId, done }
    }
}

async function loadDialogs(ctx) {
    const { client, postId, userName, user, hasRole } = ctx

    const el = $1(`#dialogs`)
    if (!el) return

    if (!alreadyMounted(el)) {
        mount(el, QuestionDialogs, { postId, userName, user, bus:pageBus })
    }
}

const QuestionAside = {
    template:`
        <div v-if="isModerator" class="mt-2 flex justify-center">
            <svg class="mr-1 align-sub text-gray-400 hover:text-gray-500 w-6 h-6 inline-block cursor-pointer" @click="deleteQuestion" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 48 48"><title>Delete question</title><g fill="none" stroke="currentColor" stroke-linejoin="round" stroke-width="4"><path d="M9 10v34h30V10z"/><path stroke-linecap="round" d="M20 20v13m8-13v13M4 10h40"/><path d="m16 10l3.289-6h9.488L32 10z"/></g></svg>
        </div>
    `,
    props:['id'],
    setup(props) {
        const { hasRole } = useAuth()
        const isModerator = hasRole('Moderator')
        const client = useClient()
        async function deleteQuestion() {
            if (confirm('Are you sure?')) {
                const api = await client.api(new DeleteQuestion({ id:props.id }))
                if (api.succeeded) {
                    location.href = '/questions'
                }
            }
        }
        return { deleteQuestion, isModerator }
    }
}

const Comments = {
    template:`
        <div v-if="editing" class="mt-4 pb-1 flex w-full">
            <div class="w-full">
                <div v-if="error" class="text-sm pb-2 text-red-500">{{error}}</div>
                <div class="w-full flex-grow relative">
                    <button type="button" @click="close" title="Discard comment"
                            :class="['absolute top-1 right-1 bg-white dark:bg-black','rounded-md text-gray-400 hover:text-gray-500 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 dark:ring-offset-black']">
                      <span class="sr-only">Close</span>
                      <svg class="h-4 w-4" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"
                           aria-hidden="true">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
                      </svg>
                    </button>
                    <textarea ref="input" class="w-full h-[4.5rem] text-sm" @keydown="keyDown" v-model="txt"></textarea>
                </div>
            </div>
            <div class="pl-2">
                <PrimaryButton class="whitespace-nowrap" @click="submit" :disabled="txt.length<15 || loading">Add Comment</PrimaryButton>
                <Loading v-if="loading" class="pt-2 !mb-0" />
                <div v-else-if="txt.length > 0 && txt.length<15" class="mt-1 text-sm text-gray-400">
                    {{15-txt.length}} characters to go
                </div>
            </div>
        </div>
        <div>
            <div v-if="comments.length" class="border-t border-gray-200 dark:border-gray-700">
                <div v-for="comment in comments" :data-createdby="comment.createdBy" :data-created="comment.created" class="py-2 border-b border-gray-100 dark:border-gray-800 text-sm text-gray-600 dark:text-gray-300 prose prose-comment">
                    <div class="inline-block mr-1 min-w-[12px]" v-if="comment.upVotes">
                        <span class="text-red-600 font-semibold">{{comment.upVotes}}</span>
                    </div>
                    <div class="inline-block mr-1">
                        <div class="flex flex-col">
                            <svg v-if="comment.createdBy !== userName" :class="['w-4 h-4',hasVoted(comment) ? 'text-red-600' : 'cursor-pointer text-gray-400 hover:text-red-600']" @click="voteUp(comment)" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 15 15"><path fill="currentColor" d="m7.5 3l7.5 8H0z"/></svg>
                            <svg class="cursor-pointer w-4 h-4 text-gray-400 hover:text-red-600" @click="flag(comment)" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="currentColor" d="M4 5a1 1 0 0 1 .3-.714a6 6 0 0 1 8.213-.176l.351.328a4 4 0 0 0 5.272 0l.249-.227c.61-.483 1.527-.097 1.61.676L20 5v9a1 1 0 0 1-.3.714a6 6 0 0 1-8.213.176l-.351-.328A4 4 0 0 0 6 14.448V21a1 1 0 0 1-1.993.117L4 21z"/></svg>
                        </div>
                    </div>
                    <svg v-if="isModerator || comment.createdBy === userName" class="mr-1 align-sub text-gray-400 hover:text-gray-500 w-4 h-4 inline-block cursor-pointer" @click="removeComment(comment)" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 48 48"><title>Delete comment</title><g fill="none" stroke="currentColor" stroke-linejoin="round" stroke-width="4"><path d="M9 10v34h30V10z"/><path stroke-linecap="round" d="M20 20v13m8-13v13M4 10h40"/><path d="m16 10l3.289-6h9.488L32 10z"/></g></svg>
                    <span v-html="comment.body"></span>
                    <span class="inline-block">
                        <span class="px-1" aria-hidden="true">&middot;</span>
                        <span class="text-indigo-700">{{comment.createdBy}}</span>
                        <span class="ml-1 text-gray-400"> {{formatDate(comment.created)}}</span>
                    </span>
                </div>
            </div>
            <div v-if="!editing" @click="startEditing" class="pt-2 text-sm cursor-pointer select-none text-indigo-700 dark:text-indigo-300 hover:text-indigo-500" title="Add a comment">add comment</div>
        </div>
    `,
    props:['id'],
    setup(props) {
        const { user, hasRole } = useAuth()
        const userName = user.value?.userName
        const isModerator = hasRole('Moderator')
        const client = useClient()
        const loading = client.loading
        const editing = ref(false)
        const txt = ref('')
        const comments = ref(getComments(props.id))
        const error = ref('')
        const input = ref()
        const instance = getCurrentInstance()
        const postId = parseInt(leftPart(props.id, '-'))
        
        pageBus.subscribe('meta:load', () => {
            comments.value = getComments(props.id)
            instance?.proxy?.$forceUpdate()
        })
        pageBus.subscribe('userPostData:load', () => {
            instance?.proxy?.$forceUpdate()
        })

        function hasVoted(comment) { 
            return userPostVotes.upVoteIds.includes(`${props.id}-${comment.created}`) 
        }
        
        function keyDown(e) {
            if (e.key === 'Enter') {
                e.preventDefault()
                return false
            } else if (e.key === 'Escape') {
                close()
            }
        }
        
        function close() {
            editing.value = false
            txt.value = ''
        }
        
        async function submit() {
            const api = await client.api(new CreateComment({ id: `${props.id}`, body: txt.value }))
            if (api.succeeded) {
                txt.value = ''
                editing.value = false
                comments.value = api.response.comments || []
                close()
            } else {
                error.value = api.errorMessage
            }
        }
        
        async function removeComment(comment) {
            if (confirm('Are you sure?')) {
                const api = await client.apiVoid(new DeleteComment({ 
                    id:`${props.id}`, 
                    created: comment.created,
                    createdBy: comment.createdBy,
                }))
                if (api.succeeded) {
                    comments.value = api.response.comments || []
                    close()
                }
            }
        }
        
        function startEditing() {
            editing.value = true
            nextTick(() => input.value.focus())
        }

        async function voteUp(comment) {
            const refId = `${props.id}-${comment.created}`
            const api = await client.apiVoid(new CommentVote({ refId, created: comment.created, up: true }))
            if (api.succeeded) {
                await updateUserData(postId)
            }
        }

        function flag(comment) {
            pageBus.publish('showReportDialog', `${props.id}-${comment.created}`)
        }
        
        return { editing, userName, isModerator, txt, input, comments, keyDown, startEditing, 
                formatDate, loading, error, submit, close, removeComment, voteUp, flag, hasVoted }
    }
}

const ContentFeatures = {
    components: {
        Comments,
        ShareDialog,
    },
    template: `
        <div class="relative mt-4 text-sm">
            <ShareDialog v-if="show == 'share'" :refId="id" @done="show=''" />
            <span @click="share" class="share-link mr-2 cursor-pointer select-none text-indigo-700 dark:text-indigo-300 hover:text-indigo-500" title="Share this Question">share</span>
            <span @click="toggleEdit" class="edit-link mr-2 cursor-pointer select-none text-indigo-700 dark:text-indigo-300 hover:text-indigo-500" title="Edit this Question">{{editing ? 'close' : 'edit'}}</span>
            <span @click="flag" class="flag-link mr-2 cursor-pointer select-none text-indigo-700 dark:text-indigo-300 hover:text-indigo-500" title="Flag this Question">flag</span>
        </div>
        <div class="mt-4">
            <Comments :id="id" />
        </div>
    `,
    props:['bus','id'],
    setup(props) {
        const { user, hasRole } = useAuth()
        const editing = ref(false)
        const isQuestion = props.id.indexOf('-') < 0
        const article = isQuestion
            ? $1(`[data-postid="${props.id}"]`)
            : $1(`[data-answer="${props.id}"]`)
        const preview = article.querySelector('.preview'),
              edit = article.querySelector('.edit')
        const show = ref('')

        function share() {
            show.value = show.value === 'share' ? '' : 'share'
        }
        
        function toggleEdit() {
            if (!user.value?.userName) {
                location.href = signInUrl()
                return
            }
            
            props.bus.publish('toggleEdit')
            editing.value = !editing.value
            if (editing.value) {
                article.scrollIntoView({ behavior: 'smooth' })
                setTimeout(() => globalThis?.hljs?.highlightAll(), 1)
            }
        }
        
        function flag() {
            show.value = ''
            pageBus.publish('showReportDialog', props.id)
        }

        props.bus.subscribe('editDone', dto => {
            editing.value = false
        })

        onMounted(() => {
            const el = $1(`[data-comments='${props.id}']`)
            el.style.display = 'none'
            el.innerHTML = ''
            preview.classList.add('hidden')
            edit.classList.remove('hidden')
        })

        return { editing, show, share, toggleEdit, flag }
    }
}

const EditQuestion = {
    components: { ContentFeatures },
    template:`
    <div v-if="editing">
        <div v-if="user?.userName">
            <div v-if="request.body">
                <Alert class="mb-2" v-if="!canUpdate">You need at least 10 reputation to Edit other User's Questions.</Alert>
                <AutoForm ref="autoform" type="UpdateQuestion" v-model="request" header-class="" submit-label="Update Question" 
                    :configureField="configureField" @success="onSuccess">
                    <template #heading>
                        <div class="pt-4 pb-2 px-6 flex justify-between">
                            <h3 class="text-2xl font-semibold">Edit Question</h3>
                            <div>
                                <img class="h-6 w-6 sm:h-8 sm:w-8 rounded-full bg-contain" :src="'/avatar/' + user.userName" :alt="user.userName">
                            </div>
                        </div>
                    </template>
                    <template #leftbuttons>
                        <SecondaryButton @click="close">Cancel</SecondaryButton>            
                    </template>
                </AutoForm>
                <div v-if="previewHtml">
                    <h3 class="my-4 select-none text-xl font-semibold flex items-center cursor-pointer" @click="expandPreview=!expandPreview">
                        <svg :class="['w-4 h-4 inline-block mr-1 transition-all',!expandPreview ? '-rotate-90' : '']" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="currentColor" d="M11.178 19.569a.998.998 0 0 0 1.644 0l9-13A.999.999 0 0 0 21 5H3a1.002 1.002 0 0 0-.822 1.569z"/></svg>
                        Preview
                    </h3>
                    <div v-if="expandPreview" class="border-t border-gray-200 pt-4">
                        <div id="question" class="flex-grow prose" v-html="previewHtml"></div>
                    </div>
                </div>
            </div>
            <div v-else>
                <Loading />
            </div>
        </div>
    </div>
    <div v-else>
        <div v-html="savedHtml" class="xl:flex-grow prose"></div>
        <div class="pt-6 flex flex-1 items-end">
        
            <div class="flex justify-between w-full items-center">
                <div class="flex-grow">
                    <div class="flex space-x-4 divide-x divide-gray-200 dark:divide-gray-800 text-sm sm:space-x-6 w-full">
                        <div class="flex flex-wrap gap-x-2 gap-y-2">
                            <a v-for="tag in request.tags" :href="'questions/tagged/' + encodeURIComponent(tag)" class="inline-flex items-center rounded-md bg-blue-50 dark:bg-blue-900 hover:bg-blue-100 dark:hover:bg-blue-800 px-2 py-1 text-xs font-medium text-blue-700 dark:text-blue-200 ring-1 ring-inset ring-blue-700/10">{{tag}}</a>
                        </div>
                    </div>
                </div>
                <div class="ml-2 text-xs">
                    <div v-if="request.lastEditDate ?? request.creationDate" class="flex">
                        <span>{{request.lastEditDate ? "edited" : "created"}}</span>
                        <dd class="ml-1 text-gray-600 dark:text-gray-300">
                            <time :datetime="request.lastEditDate ?? request.creationDate">{{formatDate(request.lastEditDate ?? request.creationDate)}}</time>
                            <span v-if="request.modifiedBy && request.modifiedBy != request.createdBy">
                                <span> by </span><b>{{request.modifiedBy}}</b>
                            </span>
                        </dd>
                    </div>
                </div>
            </div>
        </div>
        <ContentFeatures :bus="bus" :id="id" />
    </div>
    `,
    props:['bus','id','createdBy','previewHtml'],
    emits:['done'],
    setup(props) {
        const { user, hasRole } = useAuth()
        const isModerator = hasRole('Moderator')
        const client = useClient()
        const autoform = ref()
        const editing = ref(false)
        const expandPreview = ref(true)
        const request = ref(new UpdateQuestion())
        let original = null
        request.value.id = props.id
        const previewHtml = ref(props.previewHtml || '')
        const savedHtml = ref(props.previewHtml || '')
        let allTags = localStorage.getItem('data:tags.txt')?.split('\n') || []
        const rep = document.querySelector('[data-rep]')?.dataset?.rep || 1
        const canUpdate = computed(() => rep.value >= 10 || props.createdBy === user.value?.userName || isModerator)

        const { createDebounce } = useUtils()
        let lastBody = ''
        let i = 0
        let tagsInput = null

        const debounceApi = createDebounce(async markdown => {
            if (lastBody === request.value.body) return
            lastBody = request.value.body
            const api = await client.api(new PreviewMarkdown({ markdown }))
            if (api.succeeded) {
                previewHtml.value = api.response
                nextTick(() => globalThis?.hljs?.highlightAll())
            }
        }, 100)

        watchEffect(async () => {
            debounceApi(request.value.body)
        })

        async function onSuccess(r) {
            const api = await client.api(new PreviewMarkdown({ markdown:request.value.body }))
            if (api.succeeded) {
                savedHtml.value = api.response
                setTimeout(() => globalThis?.hljs?.highlightAll(), 1)
                editing.value = false
                props.bus.publish('editDone', request.value)
            }
        }

        onMounted(async () => {
            const footer = $1('.question-footer')
            if (footer) {
                footer.classList.add('hidden')
                footer.innerHTML = ''
            }

            props.bus.subscribe('toggleEdit', () => {
                editing.value = !editing.value
            })

            const api = await client.api(new GetQuestionFile({ id: props.id }))
            if (api.succeeded) {
                original = JSON.parse(api.response)
                Object.assign(request.value, original)
            }

            nextTick(applyGlobalChanges)
        })

        function close() {
            Object.assign(request.value, original) //revert changes
            editing.value = false
            props.bus.publish('editDone')
        }

        function configureField(inputProp) {
            if (inputProp.type === 'tag') {
                tagsInput = inputProp
                inputProp.allowableValues = allTags
            }
        }

        return { user, editing, canUpdate, request, previewHtml, savedHtml, autoform, expandPreview,
            configureField, onSuccess, close, formatDate, getUserRep }
    }
}



async function loadEditQuestion(ctx) {
    const { client, postId, userName, user, hasRole } = ctx

    const el = $1(`[data-postid="${postId}"]`)
    if (!el) return

    const id = el.dataset.postid
    const question = el,
        edit = el.querySelector('.edit'),
        title = el.querySelector('h1 span'),
        preview = el.querySelector('.preview'),
        previewHtml = preview?.innerHTML,
        questionAside = el.querySelector('.question-aside'),
        features = el.querySelector(`[data-question='${id}']`)

    if (!features) return // Locked Questions

    const bus = new EventBus()
    bus.subscribe('editDone', dto => {
        if (dto) {
            title.innerHTML = dto.title
        } 
    })
    
    if (questionAside) {
        mount(questionAside, QuestionAside, { id:postId })
    } else {
        console.warn(`could not find .question-aside'`)
    }
    if (edit) {
        mount(edit, EditQuestion, { bus, id:`${postId}`, createdBy:question.dataset.createdby, previewHtml })
    } else {
        console.warn(`could not find .edit'`)
    }
}

const EditAnswer = {
    components: { ContentFeatures },
    template:`
    <div v-if="editing">
        <div v-if="user?.userName">
            <div v-if="request.body">
                <Alert class="mb-2" v-if="!canUpdate">You need at least 100 reputation to Edit other User's Answers.</Alert>
                <AutoForm ref="autoform" type="UpdateAnswer" v-model="request" header-class="" submit-label="Update Answer" @success="onSuccess">
                    <template #heading>
                        <div class="pt-4 pb-2 px-6 flex justify-between">
                            <h3 class="text-2xl font-semibold">Edit Answer</h3>
                            <div>
                                <img class="h-6 w-6 sm:h-8 sm:w-8 rounded-full bg-contain" :src="'/avatar/' + user.userName" :alt="user.userName">
                            </div>
                        </div>
                    </template>
                    <template #leftbuttons>
                        <SecondaryButton @click="close">Cancel</SecondaryButton>            
                    </template>
                </AutoForm>
                <div v-if="previewHtml">
                    <h3 class="my-4 select-none text-xl font-semibold flex items-center cursor-pointer" @click="expandPreview=!expandPreview">
                        <svg :class="['w-4 h-4 inline-block mr-1 transition-all',!expandPreview ? '-rotate-90' : '']" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="currentColor" d="M11.178 19.569a.998.998 0 0 0 1.644 0l9-13A.999.999 0 0 0 21 5H3a1.002 1.002 0 0 0-.822 1.569z"/></svg>
                        Preview
                    </h3>
                    <div v-if="expandPreview" class="border-t border-gray-200 pt-4">
                        <div id="question" class="flex-grow prose" v-html="previewHtml"></div>
                    </div>
                </div>
            </div>
            <div v-else>
                <Loading />
            </div>
        </div>
        <div v-else>
            <div class="shadow sm:rounded-md">
                <div class="py-4 px-6">
                    <h3 class="mb-4 text-2xl font-semibold">Edit Answer</h3>
                    <MarkdownInput v-model="previewHtml" />
                    <div class="mt-4 flex justify-center">
                        <SecondaryButton :href="signInUrl()">Sign In to Answer</SecondaryButton>
                    </div>
                </div>
            </div>
        </div>
    </div>
    <div v-else>
        <div v-html="savedHtml" class="xl:flex-grow prose"></div>
        
        <div v-if="answer" class="pt-6 flex flex-1 items-end">
            <div class="flex justify-end w-full">
                <div class="text-xs">
                    <div v-if="answer.lastEditDate || answer.creationDate" class="flex">
                        <span>{{answer.lastEditDate ? "edited" : "created"}}</span>
                        <dd class="ml-1 text-gray-600 dark:text-gray-300">
                            <time :datetime="answer.lastEditDate ?? answer.creationDate">{{formatDate(answer.lastEditDate ?? answer.creationDate)}}</time>
                            <span v-if="answer.modifiedBy && answer.modifiedBy != answer.createdBy">
                                <span> by </span><b>{{answer.modifiedBy}}</b>
                            </span>
                        </dd>
                    </div>
                </div>
            </div>
        </div>
        <ContentFeatures :bus="bus" :id="id" />
    </div>
    `,
    props:['bus','id','createdBy','previewHtml'],
    setup(props) {

        const { user, hasRole } = useAuth()
        const isModerator = hasRole('Moderator')
        const rep = document.querySelector('[data-rep]')?.dataset?.rep || 1
        const canUpdate = computed(() => rep.value >= 100 || props.createdBy === user.value?.userName || isModerator)
        const client = useClient()
        const autoform = ref()
        const editing = ref(false)
        const expandPreview = ref(true)
        const answer = ref()
        const request = ref(new AnswerQuestion(props))
        request.value.postId = props.id
        const previewHtml = ref(props.previewHtml || '')
        const savedHtml = ref(props.previewHtml || '')

        const { createDebounce } = useUtils()
        let lastBody = ''

        const debounceApi = createDebounce(async markdown => {
            if (lastBody === request.value.body) return
            lastBody = request.value.body
            const api = await client.api(new PreviewMarkdown({ markdown }))
            if (api.succeeded) {
                previewHtml.value = api.response
                nextTick(() => globalThis?.hljs?.highlightAll())
            }
        }, 100)

        watchEffect(async () => {
            debounceApi(request.value.body)
        })

        async function onSuccess(r) {
            const api = await client.api(new PreviewMarkdown({ markdown:request.value.body }))
            if (api.succeeded) {
                savedHtml.value = api.response
                setTimeout(() => globalThis?.hljs?.highlightAll(), 1)
                close()
            }
        }

        onMounted(async () => {
            props.bus.subscribe('toggleEdit', () => {
                editing.value = !editing.value
            })

            const api = await client.api(new GetAnswer({ id: props.id }))
            if (api.succeeded) {
                answer.value = api.response?.result
                request.value.body = answer.value.body || ''
                nextTick(() => globalThis?.hljs?.highlightAll())
            }
        })

        function close() {
            editing.value = false
            props.bus.publish('editDone')
        }
        
        async function remove() {
            if (confirm('Are you sure you want to delete this Comment?')) {
                client.apiVoid(new DeleteComment({ id:props.id }))
                    .then(() => location.reload())
            }
        }

        return { editing, user, answer, isModerator, canUpdate, request, previewHtml, savedHtml, autoform, expandPreview, 
            signInUrl, onSuccess, close, formatDate }
    }
}

async function loadEditAnswers(ctx) {
    const { client, postId, userName, user, hasRole } = ctx
    
    const isModerator = hasRole('Moderator')
    const sel = `[data-answer]`
    
    $$(sel).forEach(el => {
        const id = el.dataset.answer
        const answer = el,
            edit = el.querySelector('.edit'),
            preview = el.querySelector('.preview'),
            previewHtml = preview?.innerHTML,
            footer = el.querySelector('.answer-footer')

        const bus = new EventBus()

        const answerId = answer.dataset.answer
        if (edit) {
            mount(edit, EditAnswer, { bus, id:answerId, createdBy:answer.dataset.createdby, previewHtml })

            if (footer) {
                footer.classList.add('hidden')
                footer.innerHTML = ''
            }
        } else {
            console.warn(`could not find .edit'`)
        }
    })
}

function getUserRep(userName) {
    return userReputations[userName] || ''
}

async function loadUserReputations(userName) {

    const userNames = new Set()
    if (userName) userNames.add(userName)
    const createdBy = $1('#question')?.dataset?.createdby
    if (createdBy) userNames.add(createdBy)
    if (meta?.createdBy) userNames.add(meta.createdBy)
    
    $$('[data-rep-user]').forEach(x => {
        userNames.add(x.dataset.repUser)
    })
    if (userNames.size > 0) {
        const api = await client.api(new GetUserReputations({ userNames: Array.from(userNames) }))
        if (api.succeeded) {
            userReputations = api.response.results
            pageBus.publish('userReputations:load')
            Object.keys(api.response.results).forEach(userName => {
                $$(`[data-rep-user="${userName}"]`).forEach(el => {
                    el.innerHTML = userReputations[userName] || 1
                })
            })
            if (userName) {
                $$('[data-rep]').forEach(el => {
                    el.innerHTML = userReputations[userName] || 1
                })
            }
        }
    }
}

export default  {
    async load() {
        const { user, hasRole } = useAuth()
        const userName = user.value?.userName
        const postId = parseInt($1('[data-postid]')?.getAttribute('data-postid'))

        if (!localStorage.getItem('data:tags.txt')) {
            fetch('/data/tags.txt')
                .then(r => r.text())
                .then(txt => localStorage.setItem('data:tags.txt', txt))
        }
        
        if (meta == null) {
            client.get(new GetMeta({ id:`${postId}` }))
                .then(r => {
                    meta = r
                    pageBus.publish('meta:load')
                })
        }
        
        if (!isNaN(postId)) {
            const ctx = { client, userName, postId, user, hasRole }
            await Promise.all([
                loadDialogs(ctx),
                loadVoting(ctx),
                loadEditQuestion(ctx),
                loadEditAnswers(ctx),
                loadUserReputations(userName)
            ])
        }
    }
}
