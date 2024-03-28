import { ref, watchEffect, nextTick, onMounted } from "vue"
import { queryString } from "@servicestack/client"
import { useClient, useAuth, useUtils } from "@servicestack/vue"
import { AnswerQuestion, PreviewMarkdown } from "dtos.mjs"

export default {
    template:`
        <div v-if="user?.userName">
            <AutoForm ref="autoform" type="AnswerQuestion" v-model="request" header-class="" submit-label="Submit Answer" @success="onSuccess">
                <template #heading>
                    <div class="pt-4 px-6 flex justify-between">
                        <h3 class="text-2xl font-semibold">Your Answer</h3>
                        <div>
                            <img class="h-6 w-6 sm:h-8 sm:w-8 rounded-full bg-contain" :src="'/avatar/' + user.userName" :alt="user.userName">
                        </div>
                    </div>
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
    props:['id','title','body','refId'],
    setup(props) {
        
        const { user } = useAuth()
        const client = useClient()
        const autoform = ref()
        const request = ref(new AnswerQuestion(props))
        const qs = queryString(location.search)
        request.value.postId = props.id
        if (qs.title) request.value.title = qs.title
        if (qs.body) request.value.body = qs.body
        if (qs.refId || qs.refid) request.value.refId = qs.refId || qs.refid
        const previewHtml = ref('')
        const expandPreview = ref(true)

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
        
        function onSuccess(r) {
            location.reload()
        }
        
        const signInUrl = () => `/Account/Login?ReturnUrl=${location.pathname}`
        
        return { user, request, previewHtml, expandPreview, autoform, signInUrl, onSuccess }
    }
}
