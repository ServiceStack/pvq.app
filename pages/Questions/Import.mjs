import { ref, watch, computed, onMounted, nextTick } from 'vue'
import { useAuth, useUtils } from "@servicestack/vue"

const ImportDialog = {
    template:`
  <div v-if="show" :class="[transition1,'absolute top-4 left-0 z-10 min-w-[350px] p-4 mt-2 w-56 origin-top-left rounded-md bg-white shadow-lg ring-1 ring-black ring-opacity-5 focus:outline-none']" role="menu" aria-orientation="vertical" aria-labelledby="menu-button" tabindex="-1">
    <div class="py-1" role="none">
        <div>
            <b>Import a question from {{site}}</b>
            <button type="button" @click="show=false" title="Close" class="absolute top-2 right-2 bg-white dark:bg-black','rounded-md text-gray-400 hover:text-gray-500 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 dark:ring-offset-black">
              <span class="sr-only">Close</span>
              <svg class="h-4 w-4" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
            </button>            
        </div>
        <div class="py-4">
            <TextInput id="importUrl" label="" v-model="url" :placeholder="'https://...     URL to question on ' + site" />
        </div>
        <div class="flex justify-end">
            <PrimaryButton @click="importUrl">Import</PrimaryButton>
        </div>
    </div>
  </div>
    `,
    emits:['done'],
    props: {
        site:String,
    },
    setup(props, { emit }) {
        const { transition } = useUtils()

        const show = ref(false)
        const url = ref('')
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

        function done() {
            emit('done')
        }
        
        function importUrl() {
            if (url) {
                location.href = `/questions/ask?import=${encodeURIComponent(url.value)}&site=${props.site}`
            }
            emit('done')
        }
        
        onMounted(async () => {
            const text = await navigator.clipboard.readText()
            if (text.startsWith('https://')) {
                url.value = text
            }
            
            nextTick(() => {
                document.getElementById('importUrl')?.focus()
            })
        })

        return { show, transition1, url, importUrl, done }
    }
}

export default {
    components: { ImportDialog },
    template:`
        <div class="relative">
            <div class="mb-2 text-right">
                <span class="text-gray-600">import from</span>
            </div>
            <div class="flex gap-2">
                <SecondaryButton title="Import post from StackOverflow" @click="toggle('StackOverflow')">
                    <svg class="w-5 h-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 128 128"><path fill="#bbb" d="M101.072 82.51h11.378V128H10.05V82.51h11.377v34.117h79.644zm0 0"/><path fill="#f58025" d="m33.826 79.13l55.88 11.738l2.348-11.166l-55.876-11.745Zm7.394-26.748l51.765 24.1l4.824-10.349l-51.768-24.1Zm14.324-25.384L99.428 63.53l7.309-8.775l-43.885-36.527ZM83.874 0l-9.167 6.81l34.08 45.802l9.163-6.81Zm-51.07 105.254h56.89V93.881h-56.89Zm0 0"/></svg>
                </SecondaryButton>
                <SecondaryButton title="Import post from Reddit" @click="toggle('Reddit')">
                    <svg class="w-5 h-5" xmlns="http://www.w3.org/2000/svg" width="1em" height="1em" viewBox="0 0 256 256"><circle cx="128" cy="128" r="128" fill="#ff4500"/><path fill="#fff" d="M213.15 129.22c0-10.376-8.391-18.617-18.617-18.617a18.74 18.74 0 0 0-12.97 5.189c-12.818-9.157-30.368-15.107-49.9-15.87l8.544-39.981l27.773 5.95c.307 7.02 6.104 12.667 13.278 12.667c7.324 0 13.275-5.95 13.275-13.278c0-7.324-5.95-13.275-13.275-13.275c-5.188 0-9.768 3.052-11.904 7.478l-30.976-6.562c-.916-.154-1.832 0-2.443.458c-.763.458-1.22 1.22-1.371 2.136l-9.464 44.558c-19.837.612-37.692 6.562-50.662 15.872a18.74 18.74 0 0 0-12.971-5.188c-10.377 0-18.617 8.391-18.617 18.617c0 7.629 4.577 14.037 10.988 16.939a33.598 33.598 0 0 0-.458 5.646c0 28.686 33.42 52.036 74.621 52.036c41.202 0 74.622-23.196 74.622-52.036a35.29 35.29 0 0 0-.458-5.646c6.408-2.902 10.985-9.464 10.985-17.093M85.272 142.495c0-7.324 5.95-13.275 13.278-13.275c7.324 0 13.275 5.95 13.275 13.275s-5.95 13.278-13.275 13.278c-7.327.15-13.278-5.953-13.278-13.278m74.317 35.251c-9.156 9.157-26.553 9.768-31.588 9.768c-5.188 0-22.584-.765-31.59-9.768c-1.371-1.373-1.371-3.51 0-4.883c1.374-1.371 3.51-1.371 4.884 0c5.8 5.8 18.008 7.782 26.706 7.782c8.699 0 21.058-1.983 26.704-7.782c1.374-1.371 3.51-1.371 4.884 0c1.22 1.373 1.22 3.51 0 4.883m-2.443-21.822c-7.325 0-13.275-5.95-13.275-13.275s5.95-13.275 13.275-13.275c7.327 0 13.277 5.95 13.277 13.275c0 7.17-5.95 13.275-13.277 13.275"/></svg>
                </SecondaryButton>
                <SecondaryButton title="Import question from Discourse" @click="toggle('Discourse')">
                    <svg class="w-5 h-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 259"><path d="M129.095 0C59.01 0 0 56.82 0 126.93v131.434l129.07-.124c70.085 0 126.93-59.01 126.93-129.095C256 59.06 199.106 0 129.095 0"/><path fill="#fff9ae" d="M130.34 49.13a78.696 78.696 0 0 0-69.165 116.153L46.94 211.077l51.12-11.548c29.274 13.189 63.625 7.265 86.79-14.967c23.166-22.231 30.497-56.31 18.523-86.1c-11.974-29.792-40.85-49.317-72.958-49.333z"/><path fill="#00aeef" d="M191.857 176.492c-22.347 28.19-60.971 37.625-93.798 22.912l-51.12 11.698l52.041-6.148c34.5 20.21 78.672 11.318 102.665-20.666c23.993-31.985 20.17-76.88-8.886-104.347c21.816 28.603 21.444 68.36-.902 96.55"/><path fill="#00a94f" d="M187.456 161.546c-19.28 30.369-56.707 43.785-90.89 32.582L46.94 211.102l51.12-11.573c36.408 16.446 79.361 2.983 99.87-31.3c20.508-34.285 12.054-78.497-19.655-102.798c24.681 26.169 28.462 65.747 9.182 96.115"/><path fill="#f15d22" d="M65.88 167.025c-14.25-34.345-2.508-73.973 28.15-95.012c30.657-21.04 71.857-17.743 98.779 7.903c-24.934-32.72-70.866-40.708-105.381-18.324c-34.515 22.384-45.958 67.58-26.253 103.69L46.94 211.078z"/><path fill="#d0232b" d="M61.175 165.283c-17.679-32.655-10.117-73.225 18.138-97.318c28.255-24.094 69.51-25.15 98.961-2.534c-28.251-29.748-74.62-32.792-106.518-6.993c-31.898 25.798-38.616 71.778-15.434 105.625l-9.358 47.039z"/></svg>
                </SecondaryButton>
            </div>
            <div class="mt-1 absolute top-12 left-1 -ml-44">
                <ImportDialog class="" v-if="site" :site="site" @done="done" />
            </div>
        </div>
    `,
    setup(props) {
        const site = ref('')
        
        function toggle(newSite) {
            site.value = site.value === newSite
                ? ''
                : newSite
        }
        
        function done() {
            site.value = ''
        }
        
        return { site, toggle, done }
    }
}