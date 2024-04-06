import { ref, computed, watchEffect, nextTick, onMounted } from "vue"
import { $$, $1, on, JsonServiceClient, EventBus, toDate } from "@servicestack/client"
import { useClient, useAuth, useUtils } from "@servicestack/vue"
import { mount, alreadyMounted } from "app.mjs"
import {
    UserPostData, PostVote, GetQuestionFile,
    AnswerQuestion, UpdateQuestion, PreviewMarkdown, GetAnswerBody, CreateComment, GetMeta,
    DeleteQuestion, DeleteComment, GetUserReputations,
} from "dtos.mjs"

const client = new JsonServiceClient()
let meta = null

function getComments(id) {
    if (!meta) return []
    return meta.comments && meta.comments[id] || []
}
const signInUrl = () => `/Account/Login?ReturnUrl=${location.pathname}`

function formatDate(date) {
    const d = toDate(date)
    return d.getDate() + ' ' + d.toLocaleString('en-US', { month: 'short' }) + ' at '
        + `${d.getHours()}`.padStart(2,'0')+ `:${d.getMinutes()}`.padStart(2,'0')
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



async function loadVoting(ctx) {
    const { client, postId, userName, user, hasRole } = ctx

    let userPostVotes = {upVoteIds:[], downVoteIds:[]}
    let origPostValues = {upVoteIds:[], downVoteIds:[]}
    function updateVote(el) {
        const up = el.querySelector('.up')
        const down = el.querySelector('.down')
        const score = el.querySelector('.score')

        const value = getValue(userPostVotes, el.dataset.refid)
        up.classList.toggle('text-green-600',value === 1)
        up.innerHTML = value === 1 ? svgPaths.up.solid : svgPaths.up.empty
        down.classList.toggle('text-green-600',value === -1)
        down.innerHTML = value === -1 ? svgPaths.down.solid : svgPaths.down.empty
        score.innerHTML = parseInt(score.dataset.score) + value - getValue(origPostValues, el.dataset.refid)
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
                loadUserReputations(ctx)
                setTimeout(() => loadUserReputations(ctx), 5000)
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

    const api = await client.api(new UserPostData({ postId }))
    if (api.succeeded) {
        origPostValues = api.response
        userPostVotes = Object.assign({}, origPostValues)
        $$('.voting').forEach(updateVote)
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

const AddComment = {
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
    props:['id','bus'],
    setup(props) {
        const { user, hasRole } = useAuth()
        const userName = user.value?.userName
        const isModerator = hasRole('Moderator')
        const client = useClient()
        const loading = client.loading
        const txt = ref('')
        const editing = ref(true)
        const comments = ref(getComments(props.id))
        const error = ref('')
        const input = ref()
        
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
        
        onMounted(() => {
            input.value.focus()
        })
        
        return { userName, isModerator, txt, input, editing, comments, keyDown, formatDate, loading, error, submit, close, removeComment, startEditing }
    }
}

const EditQuestion = {
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
            <dl class="question-footer flex space-x-4 divide-x divide-gray-200 dark:divide-gray-800 text-sm sm:space-x-6 w-full">
                <div class="flex flex-wrap gap-x-2 gap-y-2">
                    <a v-for="tag in request.tags" :href="'questions/tagged/' + encodeURIComponent(tag)" class="inline-flex items-center rounded-md bg-blue-50 dark:bg-blue-900 hover:bg-blue-100 dark:hover:bg-blue-800 px-2 py-1 text-xs font-medium text-blue-700 dark:text-blue-200 ring-1 ring-inset ring-blue-700/10">{{tag}}</a>
                </div>
                <div v-if="request.lastEditDate ?? request.creationDate" class="flex flex-grow px-4 sm:px-6 text-sm justify-end">
                    <span>{{request.lastEditDate ? "edited" : "created"}}</span>
                    <dd class="ml-2 text-gray-600 dark:text-gray-300">
                        <time :datetime="request.lastEditDate ?? request.creationDate">{{formatDate(request.lastEditDate ?? request.creationDate)}}</time>
                    </dd>
                </div>
            </dl>
        </div>
    </div>
    `,
    props:['id','createdBy','previewHtml','bus'],
    setup(props) {
        const { user, hasRole } = useAuth()
        const isModerator = hasRole('Moderator')
        const client = useClient()
        const autoform = ref()
        const editing = ref(true)
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
                props.bus.publish('close', request.value)
            }
        }

        onMounted(async () => {
            props.bus.subscribe('edit', () => editing.value = true)
            props.bus.subscribe('preview', () => editing.value = false)
            const api = await client.api(new GetQuestionFile({ id: props.id }))
            if (api.succeeded) {
                original = JSON.parse(api.response)
                // console.log('original', original)
                Object.assign(request.value, original)
            }
            nextTick(() => globalThis?.hljs?.highlightAll())
        })

        function close() {
            Object.assign(request.value, original) //revert changes
            editing.value = false
            props.bus.publish('close', original)
        }

        function configureField(inputProp) {
            if (inputProp.type === 'tag') {
                tagsInput = inputProp
                inputProp.allowableValues = allTags
            }
        }

        return { user, canUpdate, request, previewHtml, savedHtml, autoform, editing, expandPreview, configureField, onSuccess, close, formatDate }
    }
}

async function loadEditQuestion(ctx) {
    const { client, postId, userName, user, hasRole } = ctx

    const el = $1(`[data-postid="${postId}"]`)
    if (!el) return

    const id = el.id
    const question = el,
        editLink = el.querySelector('.edit-link'),
        edit = el.querySelector('.edit'),
        title = el.querySelector('h1 span'),
        footer = el.querySelector('.question-footer'),
        preview = el.querySelector('.preview'),
        previewHtml = preview.innerHTML,
        addCommentLink = el.querySelector('.add-comment-link'),
        comments = el.querySelector('.comments'),
        questionAside = el.querySelector('.question-aside')
    
    if (!editLink) return // Locked Questions
    editLink.innerHTML = 'edit'
    addCommentLink.innerHTML = 'add comment'
    let showEdit = false
    const bus = new EventBus()
    bus.subscribe('close', (dto) => {
        title.innerHTML = dto.title
        toggleEdit(false)
    })
    
    mount(questionAside, QuestionAside, { id:postId })

    async function toggleEdit(editMode) {
        if (editMode) {
            if (!alreadyMounted(edit)) {
                mount(edit, EditQuestion, { id:postId, createdBy:question.dataset.createdby, previewHtml, bus })
            }
            preview.classList.add('hidden')
            preview.innerHTML = ''
            edit.classList.remove('hidden')
            question.scrollIntoView({ behavior: 'smooth' })
            editLink.innerHTML = 'close'
            footer.classList.add('hidden')
        } else {
            editLink.innerHTML = 'edit'
        }
        bus.publish(editMode ? 'edit' : 'preview')
        showEdit = editMode
    }

    on(editLink, {
        click() {
            if (!userName) {
                location.href = signInUrl()
            } else {
                toggleEdit(!showEdit)
            }
        }
    })

    on(addCommentLink, {
        click() {
            if (!userName) {
                location.href = signInUrl()
            } else {
                addCommentLink.classList.add('hidden')
                comments.innerHTML = ''
                mount(comments, AddComment, { id:postId, bus })
            }
        }
    })
}

const EditAnswer = {
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
    <div v-else v-html="savedHtml" class="xl:flex-grow prose"></div>
    `,
    props:['id','createdBy','previewHtml','bus'],
    setup(props) {

        const { user, hasRole } = useAuth()
        const isModerator = hasRole('Moderator')
        const rep = document.querySelector('[data-rep]')?.dataset?.rep || 1
        const canUpdate = computed(() => rep.value >= 100 || props.createdBy === user.value?.userName || isModerator)
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
            nextTick(() => globalThis?.hljs?.highlightAll())
        })

        function close() {
            editing.value = false
            props.bus.publish('close')
        }
        
        async function remove() {
            if (confirm('Are you sure you want to delete this Comment?')) {
                client.apiVoid(new DeleteComment({ id:props.id }))
                    .then(() => location.reload())
            }
        }

        return { editing, user, isModerator, canUpdate, request, previewHtml, savedHtml, autoform, expandPreview, signInUrl, onSuccess, close }
    }
}

async function loadEditAnswers(ctx) {
    const { client, postId, userName, user, hasRole } = ctx
    
    const isModerator = hasRole('Moderator')
    const sel = `[data-answer]`
    
    $$(sel).forEach(el => {
        const id = el.id
        const answer = el,
            editLink = el.querySelector('.edit-link'),
            edit = el.querySelector('.edit'),
            preview = el.querySelector('.preview'),
            previewHtml = preview.innerHTML,
            addCommentLink = el.querySelector('.add-comment-link'),
            comments = el.querySelector('.comments')

        if (!editLink) return // Locked Questions
        const answerId = answer.dataset.answer
        editLink.innerHTML = 'edit'
        addCommentLink.innerHTML = 'add comment'
        let showEdit = false
        const bus = new EventBus()
        bus.subscribe('close', () => toggleEdit(false))
        
        async function toggleEdit(editMode) {
            if (editMode) {
                if (!alreadyMounted(edit)) {
                    mount(edit, EditAnswer, { id:answerId, createdBy:answer.dataset.createdby, previewHtml, bus })
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
                if (!userName) {
                    location.href = signInUrl()
                } else {
                    toggleEdit(!showEdit)
                }
            }
        })

        on(addCommentLink, {
            click() {
                if (!userName) {
                    location.href = signInUrl()
                } else {
                    addCommentLink.classList.add('hidden')
                    mount(comments, AddComment, { id:answerId, bus })
                }
            }
        })

    })
}

async function loadUserReputations(ctx) {
    const { client, postId, userName, user, hasRole } = ctx

    const userNames = new Set()
    if (userName) userNames.add(userName)
    $$('[data-rep-user]').forEach(x => {
        userNames.add(x.dataset.repUser)
    })
    console.log('userNames', userNames.size, userNames)
    if (userNames.size > 0) {
        const api = await client.api(new GetUserReputations({ userNames: Array.from(userNames) }))
        if (api.succeeded) {
            const results = api.response.results
            Object.keys(api.response.results).forEach(userName => {
                $$(`[data-rep-user="${userName}"]`).forEach(el => {
                    // console.log('updating rep', userName, results[userName])
                    el.innerHTML = results[userName] || 1
                })
            })
            if (userName) {
                $$('[data-rep]').forEach(el => {
                    el.innerHTML = results[userName] || 1
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
                .then(r => meta = r)
        }
        
        if (!isNaN(postId)) {
            const ctx = { client, userName, postId, user, hasRole }
            await Promise.all([
                loadVoting(ctx),
                loadEditQuestion(ctx),
                loadEditAnswers(ctx),
                loadUserReputations(ctx)
            ])
        }
    }
}
