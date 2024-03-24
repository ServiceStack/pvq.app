import { ref, watchEffect, nextTick, onMounted } from "vue"
import { queryString } from "@servicestack/client"
import { useClient, useUtils } from "@servicestack/vue"
import { AskQuestion, PreviewMarkdown } from "dtos.mjs"

export default {
    template:`
        <AutoForm ref="autoform" type="AskQuestion" v-model="request" header-class="" submit-label="Create Question" 
            :configureField="configureField" @success="onSuccess">
            <template #heading></template>
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
        const request = ref(new AskQuestion())
        const qs = queryString(location.search)
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
            console.log(r)
            if (r.redirectTo) {
                location.href = r.redirectTo
            }
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
        
        return { request, previewHtml, autoform, configureField, onSuccess }
    }
}
