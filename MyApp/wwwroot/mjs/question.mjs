import { ref, computed, watchEffect, nextTick, onMounted } from "vue"
import { $$, $1, on, JsonServiceClient, EventBus } from "@servicestack/client"
import { useClient, useAuth, useUtils, useFormatters } from "@servicestack/vue"
import { UserPostData, PostVote, GetQuestionFile } from "dtos.mjs"
import { mount, alreadyMounted } from "app.mjs"
import { AnswerQuestion, UpdateQuestion, UpdateAnswer, PreviewMarkdown, GetAnswerBody, CreateComment, GetMeta } from "dtos.mjs"

let meta = null

function getComments(id) {
    if (!meta) return []
    return meta.comments && meta.comments[id] || []
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
        $$('.voting').forEach(updateVote)
    }
}

const AddComment = {
    template:`
        <div v-if="editing" class="mt-4 flex h-[5rem] w-full">
            <div class="w-full">
                <div v-if="error" class="text-sm pb-2 text-red-500">{{error}}</div>
                <textarea class="w-full flex-grow" @keydown="keyDown" v-model="txt"></textarea>
            </div>
            <div class="pl-2">
                <PrimaryButton class="whitespace-nowrap" @click="submit" :disabled="txt.length<15 || !client.loading">Add Comment</PrimaryButton>
                <div v-if="txt.length > 0 && txt.length<15" class="mt-1 text-sm text-gray-400">
                    {{15-txt.length}} characters to go
                </div>
            </div>
        </div>
        <div>
            <div v-if="comments.length" class="border-t border-gray-200 dark:border-gray-700">
                <div v-for="comment in comments" class="py-2 border-b border-gray-100 dark:border-gray-800 text-sm text-gray-600 dark:text-gray-300 prose prose-comment">
                    <span v-html="comment.body"></span>
                    <span class="inline-block">
                        <span class="px-1" aria-hidden="true">&middot;</span>
                        <span class="text-indigo-700">{{comment.createdBy}}</span>
                        <span class="ml-1 text-gray-400"> {{formatDate(comment.createdDate)}}</span>
                    </span>
                </div>
            </div>
            <div v-if="!editing" @click="editing=true" class="pt-2 text-sm cursor-pointer select-none text-indigo-700 dark:text-indigo-300 hover:text-indigo-500" title="Add a comment">add comment</div>
        </div>
    `,
    props:['id','bus'],
    setup(props) {
        const { formatDate } = useFormatters()
        const client = useClient()
        const txt = ref('')
        const editing = ref(true)
        const comments = ref(getComments(props.id))
        const error = ref('')
        
        function keyDown(e) {
            if (e.key === 'Enter') {
                e.preventDefault()
                return false
            } else if (e.key === 'Escape') {
                editing.value = false
            }
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
        
        return { txt, editing, comments, keyDown, submit, formatDate, client, error  }
    }
}

const EditQuestion = {
    template:`
    <div v-if="editing">
        <div v-if="user?.userName">
            <div v-if="request.body">
                <AutoForm ref="autoform" type="UpdateQuestion" v-model="request" header-class="" submit-label="Update Question" 
                    :configureField="configureField" @success="onSuccess">
                    <template #heading>
                        <div class="pt-4 px-6 flex justify-between">
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
    props:['id','previewHtml','bus'],
    setup(props) {
        const { formatDate } = useFormatters()
        const { user } = useAuth()
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

        return { user, request, previewHtml, savedHtml, autoform, editing, expandPreview, configureField, onSuccess, close, formatDate }
    }
}

async function loadEditQuestion(ctx) {
    const { client, postId, userName, user, hasRole } = ctx

    const el = $1(`[data-postid="${postId}"]`)
    if (!el) return

    const id = el.id
    const answer = el,
        editLink = el.querySelector('.edit-link'),
        edit = el.querySelector('.edit'),
        title = el.querySelector('h1 span'),
        footer = el.querySelector('.question-footer'),
        preview = el.querySelector('.preview'),
        previewHtml = preview.innerHTML,
        addCommentLink = el.querySelector('.add-comment-link'),
        comments = el.querySelector('.comments')

    if (!editLink) return // Locked Questions
    editLink.innerHTML = 'edit'
    addCommentLink.innerHTML = 'add comment'
    let showEdit = false
    const bus = new EventBus()
    bus.subscribe('close', (dto) => {
        title.innerHTML = dto.title
        toggleEdit(false)
    })

    async function toggleEdit(editMode) {
        if (editMode) {
            if (!alreadyMounted(edit)) {
                mount(edit, EditQuestion, { id:postId, previewHtml, bus })
            }
            preview.classList.add('hidden')
            preview.innerHTML = ''
            edit.classList.remove('hidden')
            answer.scrollIntoView({ behavior: 'smooth' })
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
            toggleEdit(!showEdit)
        }
    })

    on(addCommentLink, {
        click() {
            addCommentLink.classList.add('hidden')
            comments.innerHTML = ''
            mount(comments, AddComment, { id:postId, bus })
        }
    })
}

const EditAnswer = {
    template:`
    <div v-if="editing">
        <div v-if="user?.userName">
            <div v-if="request.body">
                <AutoForm ref="autoform" type="UpdateAnswer" v-model="request" header-class="" submit-label="Update Answer" @success="onSuccess">
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
    props:['id','previewHtml','bus'],
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
            nextTick(() => globalThis?.hljs?.highlightAll())
        })

        function close() {
            editing.value = false
            props.bus.publish('close')
        }

        const signInUrl = () => `/Account/Login?ReturnUrl=${location.pathname}`

        return { editing, user, request, previewHtml, savedHtml, autoform, expandPreview, signInUrl, onSuccess, close }
    }
}

async function loadEditAnswers(ctx) {
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

        on(addCommentLink, {
            click() {
                addCommentLink.classList.add('hidden')
                mount(comments, AddComment, { id:postId, bus })
            }
        })

    })
}

export default  {
    async load() {
        const client = new JsonServiceClient()
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
            await loadVoting(ctx)
            await loadEditQuestion(ctx)
            await loadEditAnswers(ctx)
        }
    }
}
