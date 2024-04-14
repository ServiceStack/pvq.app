import { ref, onMounted } from "vue"
import { useClient, useAuth } from '@servicestack/vue'
import { GetWatchedTags, WatchTags } from "dtos.mjs"

const signInUrl = () => `/Account/Login?ReturnUrl=${location.pathname}`

export default {
    template:`
        <div class="mt-8 bg-white dark:bg-black shadow sm:rounded-lg relative">
            <div class="px-4 py-5 sm:p-6">
                <div class="flex justify-between">
                    <h3 class="text-base font-semibold leading-6 text-gray-900 dark:text-gray-50">Watched Tags</h3>
                    <div v-if="edit" class="-mt-4 -mr-4">
                        <button type="button" @click="edit=false" title="Close" class="absolute top-4 right-4 bg-white dark:bg-black rounded-md text-gray-400 hover:text-gray-500 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 dark:ring-offset-black">
                            <span class="sr-only">Close</span><svg class="h-4 w-4" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path></svg>
                        </button>                    
                    </div>
                </div>
                <div v-if="edit && tags.length" class="my-4 flex gap-2 text-sm">
                    <span v-for="tag in tags" class="inline-flex items-center gap-x-0.5 rounded-md bg-blue-50 dark:bg-blue-900 px-2 py-1 text-xs font-medium text-blue-700 dark:text-blue-200 ring-1 ring-inset ring-blue-700/10">
                      {{tag}}
                      <button v-if="edit" @click="remove(tag)" type="button" class="group relative -mr-1 h-3.5 w-3.5 rounded-sm hover:bg-blue-600/20">
                        <span class="sr-only">Remove</span>
                        <svg viewBox="0 0 14 14" class="h-3.5 w-3.5 stroke-blue-700/50 group-hover:stroke-blue-700/75">
                          <path d="M4 4l6 6m0-6l-6 6" />
                        </svg>
                        <span class="absolute -inset-1"></span>
                      </button>
                    </span>
                </div>
                <div v-else-if="user" class="my-4 flex gap-2 text-sm">
                    <a v-for="tag in tags" :href="'/questions/tagged/' + encodeURIComponent(tag)" class="inline-flex items-center gap-x-0.5 rounded-md bg-blue-50 dark:bg-blue-900 px-2 py-1 text-xs font-medium text-blue-700 dark:text-blue-200 ring-1 ring-inset ring-blue-700/10 cursor-pointer">
                      {{tag}}
                    </a>
                </div>
                <div class="mt-5">
                    <div v-if="edit">
                        <div class="flex items-end w-full">
                            <div class="w-60">
                                <TagInput id="newTags" label="" v-model="newTags" :allowableValues="allTags.filter(x => !tags.includes(x))" />
                            </div>
                            <div>
                                <secondary-button @click="add" class="ml-2 w-10">Add</secondary-button>
                            </div>
                        </div>
                    </div>
                    <div v-else class="flex">
                        <span v-if="user" class="text-indigo-600 dark:text-indigo-300 hover:text-indigo-700 dark:hover:text-indigo-200 cursor-pointer" @click="edit=!edit">edit</span>
                        <a v-else :href="signInUrl()" class="text-indigo-600 dark:text-indigo-300">sign in</a>
                    </div>
                </div>
            </div>
        </div>    
    `,
    setup() {
        const client = useClient()
        const { user } = useAuth()
        const tags = ref([])
        const edit = ref()
        const newTags = ref([])
        let allTags = localStorage.getItem('data:tags.txt')?.split('\n') || []
        
        onMounted(async () => {
            if (user.userName) {
                const api = await client.api(new GetWatchedTags())
                if (api.succeeded) {
                    tags.value = api.response.results
                }
            }
        })
        
        async function refresh() {
            const api = await client.api(new GetWatchedTags())
            if (api.succeeded) {
                tags.value = api.response.results
            }
        }
        
        async function add() {
            if (newTags.length === 0) return
            const api = await client.api(new WatchTags({ subscribe:newTags.value }))
            if (api.succeeded) {
                newTags.value.forEach(tag => tags.value.push(tag))
                tags.value.sort()
                setTimeout(refresh, 2 * 1000)
                newTags.value = []
            }
        }
        
        async function remove(tag) {
            const api = await client.api(new WatchTags({ unsubscribe:[tag] }))
            if (api.succeeded) {
                tags.value = tags.value.filter(x => x !== tag)
                setTimeout(refresh, 2 * 1000)
            }
        }
        
        return { user, tags, edit, newTags, allTags, add, remove, signInUrl }
    }
}
