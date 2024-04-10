import { ref, watchEffect, nextTick, onMounted } from "vue"
import { queryString } from "@servicestack/client"
import { useClient, useUtils } from "@servicestack/vue"
import { AskQuestion, PreviewMarkdown, FindSimilarQuestions, ImportQuestion, ImportSite } from "dtos.mjs"

export default {
    template:`
        <ErrorSummary v-if="error" class="mb-2" :status="error" />
        <AutoForm ref="autoform" type="AskQuestion" v-model="request" header-class="" submit-label="Create Question" 
            :configureField="configureField" @success="onSuccess">
            <template #heading></template>
            <template #footer>
                <div v-if="similarQuestions.length" class="px-6">
                    <div class="px-4 pb-2 bg-gray-50 dark:bg-gray-900 rounded-md">
                        <div class="flex justify-between items-center">
                            <h3 class="my-4 select-none text-xl font-semibold flex items-center cursor-pointer" @click="expandSimilar=!expandSimilar">
                                <svg :class="['w-4 h-4 inline-block mr-1 transition-all',!expandSimilar ? '-rotate-90' : '']" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="currentColor" d="M11.178 19.569a.998.998 0 0 0 1.644 0l9-13A.999.999 0 0 0 21 5H3a1.002 1.002 0 0 0-.822 1.569z"/></svg>
                                Similar Questions
                            </h3>
                            <span class="text-sm text-gray-500">has this been asked before?</span>
                        </div>
                        <div v-if="expandSimilar" class="pl-4">
                            <div v-for="q in similarQuestions" :key="q.id" class="pb-2">
                                <a :href="'/questions/' + q.id + '/' + q.slug" target="_blank" class="text-indigo-600 dark:text-indigo-300 hover:text-indigo-800">{{ q.title }}</a>
                            </div>
                        </div>
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
    `,
    setup() {
        
        const client = useClient()
        const autoform = ref()
        const savedJson = localStorage.getItem('ask')
        const saved = savedJson ? JSON.parse(savedJson) : {}
        const request = ref(new AskQuestion(saved))
        const qs = queryString(location.search)
        if (qs.title) request.value.title = qs.title
        if (qs.body) request.value.body = qs.body
        if (qs.tags) request.value.tags = qs.tags.split(',')
        if (qs.refId || qs.refid) request.value.refId = qs.refId || qs.refid
        const previewHtml = ref('')
        let allTags = localStorage.getItem('data:tags.txt')?.split('\n') || []
        const expandSimilar = ref(true)
        const similarQuestions = ref([])
        const error = ref(null)

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

        watchEffect(async () => {
            if ((request.value.title ?? '').trim().length > 19) {
                findSimilarQuestions(request.value.title)
            }
        })

        let lastJson = ''        
        watchEffect(async () => {
            const json = JSON.stringify(request.value)
            if (json === lastJson) return
            localStorage.setItem('ask', json)
            lastJson = json
        })

        async function findSimilarQuestions(text) {
            const api = await client.api(new FindSimilarQuestions({ text }))
            if (api.succeeded) {
                similarQuestions.value = api.response.results || []
            }
        }
        
        function onSuccess(r) {
            localStorage.removeItem('ask')
            if (r.redirectTo) {
                location.href = r.redirectTo
            }
        }
        
        onMounted(async () => {
            
            const qs = queryString(location.search)
            if (qs.import) {
                const importQuestion = new ImportQuestion({ url: qs.import })
                const site = qs.site?.toLowerCase()
                if (site) {
                    importQuestion.site = site === 'stackoverflow'
                        ? ImportSite.StackOverflow
                        : site === 'discourse'
                            ? ImportSite.Discourse
                            : site === 'reddit'
                                ? ImportSite.Reddit
                                : null
                }
                const api = await client.api(importQuestion)
                if (api.succeeded) {
                    Object.assign(request.value, api.response.result)
                } else {
                    error.value = api.error
                }
            }
            
            if (allTags.length === 0) {
                let txt = await (await fetch('/data/tags.txt')).text()
                txt = txt.replace(/\r\n/g,'\n')
                localStorage.setItem('data:tags.txt', txt)
                allTags = txt.split('\n')
            }
        })
        
        function configureField(inputProp) {
            if (inputProp.type === 'tag') {
                tagsInput = inputProp
                inputProp.allowableValues = allTags
            }
        }
        
        return { request, error, previewHtml, autoform, expandSimilar, similarQuestions, configureField, onSuccess }
    }
}
