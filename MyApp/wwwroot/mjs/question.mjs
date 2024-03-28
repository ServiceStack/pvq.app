import { ref, watchEffect, nextTick, onMounted } from "vue"
import { $$, $1, on, JsonServiceClient, EventBus } from "@servicestack/client"
import { useClient, useAuth, useUtils } from "@servicestack/vue"
import { UserPostData, PostVote } from "dtos.mjs"
import { mount, alreadyMounted } from "app.mjs"
import { AnswerQuestion, PreviewMarkdown, GetAnswerBody } from "dtos.mjs"

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

async function loadVoting(ctx) {
    const { client, postId, userName, user, hasRole } = ctx

    let userPostVotes = {upVoteIds:[], downVoteIds:[]}
    let origPostValues = {upVoteIds:[], downVoteIds:[]}
    function updateVote(el) {
        const up = el.querySelector('.up')
        const down = el.querySelector('.down')
        const score = el.querySelector('.score')

        const value = getValue(userPostVotes, el.id)
        up.classList.toggle('text-green-600',value === 1)
        up.innerHTML = value === 1 ? svgPaths.up.solid : svgPaths.up.empty
        down.classList.toggle('text-green-600',value === -1)
        down.innerHTML = value === -1 ? svgPaths.down.solid : svgPaths.down.empty
        score.innerHTML = parseInt(score.dataset.score) + value - getValue(origPostValues, el.id)
    }
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

    $$('.voting').forEach(el => {
        const refId = el.id
        async function vote(value) {
            if (!userName) {
                location.href = `/Account/Login?ReturnUrl=${encodeURIComponent(location.pathname)}`
                return
            }

            const prevValue = getValue(userPostVotes, refId)
            setValue(refId, value)
            updateVote(el)

            const api = await client.apiVoid(new PostVote({ refId, up:value === 1, down:value === -1 }))
            if (!api.succeeded) {
                setValue(refId, prevValue)
                updateVote(el)
            }
        }

        on(el.querySelector('.up'), {
            click(e) {
                vote(getValue(userPostVotes, refId) === 1 ? 0 : 1)
            }
        })
        on(el.querySelector('.down'), {
            click(e) {
                vote(getValue(userPostVotes, refId) === -1 ? 0 : -1)
            }
        })
    })

    const api = await client.api(new UserPostData({ postId }))
    if (api.succeeded) {
        origPostValues = api.response
        userPostVotes = Object.assign({}, origPostValues)
        console.log('origPostValues', origPostValues)
        $$('.voting').forEach(updateVote)
    }
}


const EditAnswer = {
    template:`
    <div v-if="editing">
        <div v-if="user?.userName">
            <div v-if="request.body">
                <AutoForm ref="autoform" type="EditAnswer" v-model="request" header-class="" submit-label="Update Answer" @success="onSuccess">
                    <template #heading>
                        <div class="pt-4 px-6 flex justify-between">
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
    <div v-else v-html="savedHtml" class="xl:flex-grow prose"></div>
    `,
    props:['id','title','body','refId','previewHtml','bus'],
    setup(props) {

        const { user } = useAuth()
        const client = useClient()
        const autoform = ref()
        const editing = ref(true)
        const expandPreview = ref(true)
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
            props.bus.subscribe('edit', () => editing.value = true)
            props.bus.subscribe('preview', () => editing.value = false)
            const api = await client.api(new GetAnswerBody({ id: props.id }))
            request.value.body = api.response || ''
        })

        function close() {
            editing.value = false
            props.bus.publish('close')
        }

        const signInUrl = () => `/Account/Login?ReturnUrl=${location.pathname}`

        return { editing, user, request, previewHtml, savedHtml, autoform, expandPreview, signInUrl, onSuccess, close }
    }
}

async function loadEditing(ctx) {
    const { client, postId, userName, user, hasRole } = ctx
    
    const isModerator = hasRole('Moderator')
    const sel = isModerator
        ? `[data-answer]`
        : `[data-answer='${postId}-${userName}']`
    
    $$(sel).forEach(el => {
        const id = el.id
        const answer = el,
              editLink = el.querySelector('.edit-link'),
              edit = el.querySelector('.edit'),
              preview = el.querySelector('.preview'),
              previewHtml = preview.innerHTML
        const answerId = answer.dataset.answer
        editLink.innerHTML = 'edit'
        let showEdit = false
        const bus = new EventBus()
        bus.subscribe('close', () => toggleEdit(false))
        
        async function toggleEdit(editMode) {
            if (editMode) {
                if (!alreadyMounted(edit)) {
                    mount(edit, EditAnswer, { id:answerId, previewHtml, bus })
                }
                preview.classList.add('hidden')
                preview.innerHTML = ''
                edit.classList.remove('hidden')
                answer.scrollIntoView({ behavior: 'smooth' })
                editLink.innerHTML = 'close'
                
            } else {
                editLink.innerHTML = 'edit'
            }
            bus.publish(editMode ? 'edit' : 'preview')
            showEdit = editMode
        }
        
        on(editLink, {
            click() {
                toggleEdit(!showEdit)
            }
        })
        
    })
    
    console.log(JSON.stringify(user.value))
    console.log(isModerator, user.roles)
}

export default  {
    async load() {
        const client = new JsonServiceClient()
        const { user, hasRole } = useAuth()
        const userName = user.value?.userName
        const postId = parseInt($1('[data-postid]')?.getAttribute('data-postid'))
        
        if (!isNaN(postId)) {
            const ctx = { client, userName, postId, user, hasRole }
            await loadVoting(ctx)
            await loadEditing(ctx)
        }
    }
}
