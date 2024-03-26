import { ref, watchEffect, nextTick, onMounted } from "vue"
import { queryString } from "@servicestack/client"
import { useClient, useAuth, useUtils } from "@servicestack/vue"
import { AnswerQuestion, PreviewMarkdown } from "dtos.mjs"

export default {
    template:`
        <div v-if="user?.userName">
            <AutoForm ref="autoform" type="AnswerQuestion" v-model="request" header-class="" submit-label="Submit Answer" 
                :configureField="configureField" @success="onSuccess">
                <template #heading>
                    <div class="pt-4 px-6 flex justify-between">
                        <h3 class="text-2xl font-semibold">Your Answer</h3>
                        <div>
                            <img class="h-6 w-6 sm:h-8 sm:w-8 rounded-full bg-contain" :src="'/avatar/' + user.userName" :alt="user.userName">
                        </div>
                    </div>
                </template>
            </AutoForm>
            <div v-if="request.body" class="pb-40">
                <h3 class="my-4 text-xl font-semibold">Preview</h3>
                <div class="border-t border-gray-200 pt-4">
                    <div id="question" class="flex-grow prose" v-html="previewHtml"></div>
                </div>
            </div>
        </div>
        <div v-else>
            <div class="shadow sm:rounded-md">
                <div class="py-4 px-6">
                    <h3 class="mb-4 text-2xl font-semibold">Your Answer</h3>
                    <MarkdownInput v-model="previewHtml" />
                    <div class="mt-4 flex justify-center">
                        <SecondaryButton :href="signInUrl()">Sign In to Answer</SecondaryButton>
                    </div>
                </div>
            </div>
        </div>
    `,
    props:['id'],
    setup(props) {
        
        const { user } = useAuth()
        const client = useClient()
        const autoform = ref()
        const request = ref(new AnswerQuestion())
        const qs = queryString(location.search)
        request.value.postId = props.id
        if (qs.title) request.value.title = qs.title
        if (qs.body) request.value.body = qs.body
        if (qs.tags) request.value.tags = qs.tags.split(',')
        if (qs.refId || qs.refid) request.value.refId = qs.refId || qs.refid
        const previewHtml = ref('')
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
        
        function onSuccess(r) {
            location.reload()
        }
        
        onMounted(async () => {
            if (allTags.length === 0) {
                const txt = await (await fetch('/data/tags.txt')).text()
                localStorage.setItem('data:tags.txt', txt)
                allTags = txt.split('\n')
                // TODO doesn't work
                if (tagsInput != null) {
                    tagsInput.allowableValues = allTags
                }
            }
        })
        
        function configureField(inputProp) {
            if (inputProp.type === 'tag') {
                tagsInput = inputProp
                inputProp.allowableValues = allTags
            }
        }
        
        const signInUrl = () => `/Account/Login?ReturnUrl=${location.pathname}`
        
        return { user, request, previewHtml, autoform, signInUrl, configureField, onSuccess }
    }
}
