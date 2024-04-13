import { ref, computed, watch, onMounted } from "vue"
import { forceMount } from "app.mjs"
import { $1, $$, createElement, toDate, EventBus } from "@servicestack/client"
import { useClient, useUtils } from "@servicestack/vue"
import { GetLatestNotifications, GetLatestAchievements, MarkAsRead } from "dtos.mjs"

const bus = new EventBus()
const { transition } = useUtils()
const rule1 = {
    entering: { cls: 'transition ease-out duration-100', from: 'transform opacity-0 scale-95', to: 'transform opacity-100 scale-100' },
    leaving:  { cls: 'transition ease-in duration-75', from: 'transform opacity-100 scale-100', to: 'transform opacity-0 scale-95' }
}

function formatDate(date) {
    const d = toDate(date)
    return d.getDate() + ' ' + d.toLocaleString('en-US', { month: 'short' }) + ' at '
        + `${d.getHours()}`.padStart(2,'0')+ `:${d.getMinutes()}`.padStart(2,'0')
}

const NotificationsMenu = {
    template: `
<div v-if="!hide" :class="[transition1,'absolute top-12 right-0 z-10 mt-1 origin-top-right rounded-md bg-white dark:bg-gray-800 shadow-lg ring-1 ring-black ring-opacity-5 focus:outline-none w-[26rem] sm:w-[30rem]']" role="menu" aria-orientation="vertical" aria-labelledby="menu-button" tabindex="-1">
    <div class="py-1 px-2 bg-gray-50 dark:bg-gray-900 flex justify-between text-sm items-center border-b border-gray-200 dark:border-gray-700">
        <span>
            inbox
        </span>
        <div class="inline-flex shadow rounded-md">
            <button @click="filter=''" :class="['border-gray-200 dark:border-gray-800 text-sm font-medium px-2 py-1 hover:bg-gray-100 focus:z-10 focus:ring-2 focus:ring-blue-700 focus:text-blue-700 rounded-l-lg border dark:text-white', 
                    filter !== 'unread' ? 'hover:bg-gray-100 text-blue-700 dark:bg-blue-600' : 'text-gray-900 hover:text-blue-700 dark:bg-gray-700']">
                all
            </button>
            <button @click="filter='unread'" :class="['border-gray-200 dark:border-gray-800 text-sm font-medium px-2 py-1 hover:bg-gray-100 focus:z-10 focus:ring-2 focus:ring-blue-700 focus:text-blue-700 rounded-r-md border dark:text-white', 
                    filter === 'unread' ? 'hover:bg-gray-100 text-blue-700 dark:bg-blue-600' : 'text-gray-900 hover:text-blue-700 dark:bg-gray-700']">
                unread
            </button>
        </div>        
        <span class="text-indigo-600 dark:text-indigo-300 hover:text-indigo-700 dark:hover:text-indigo-200 cursor-pointer" @click="markAll">
            mark all as read
        </span>
    </div>
  <div class="max-h-[20rem] overflow-auto" role="none">
    <ul>
        <li v-for="item in filteredResults" :key="item.id" @click="goto(item)" :class="[item.read ? 'bg-gray-100 dark:bg-gray-700' : '', 
            'px-2 py-2 text-xs font-normal hover:bg-indigo-100 dark:hover:bg-indigo-800 cursor-pointer border-b border-gray-200 dark:border-gray-700']">
            <div class="flex justify-between font-semibold text-gray-500">
                <span class="">{{typeLabel(item.type)}}</span>
                <span>{{formatDate(item.createdDate)}}</span>
            </div>
            <div class="px-2 mt-1 truncate" :title="item.title"><b class="mr-1">Q</b>{{item.title}}</div>
            <div class="px-2 mt-2" :title="item.summary">{{item.summary}}</div>
        </li>
        <li v-if="!filteredResults.length">
            <div class="px-2 py-2 text-xs font-normal text-gray-500">empty</div>
        </li>
    </ul>
  </div>
</div>  
  `,
    setup(props) {
        const client = useClient()
        const show = ref(false)
        const results = ref([])
        const filter = ref('')
        const filteredResults = computed(() => filter.value === 'unread' ? results.value.filter(x => !x.read) : results.value)
        const hide = ref(true)

        const transition1 = ref('transform opacity-0 scale-95')
        let timeout = null
        bus.subscribe('toggleNotifications', () => {
            clearTimeout(timeout)
            hide.value = false
            show.value = !show.value
            if (!show.value) timeout = setTimeout(() => hide.value = true, 700)
        })
        bus.subscribe('hideNotifications', () => {
            show.value = false
            hide.value = true
        })
        watch(show, () => {
            transition(rule1, transition1, show.value)
        })

        async function updateNotifications() {
            const api = await client.api(new GetLatestNotifications())
            if (api.succeeded) {
                results.value = api.response.results || []
                toggleUnreadNotifications(api.response.hasUnread)
            }
        }

        onMounted(async () => {
            await updateNotifications()
        })
        
        const typeLabels = {
            NewComment: 'comment',
            NewAnswer: 'answer',
            QuestionMention: 'mentioned in question',
            AnswerMention: 'mentioned in answer',
            CommentMention: 'mentioned in comment',
        }
        
        function typeLabel(type) {
            return typeLabels[type] || type
        }
        
        async function markAll() {
            results.value.forEach(x => x.read = true)
            const api = await client.api(new MarkAsRead({ allNotifications: true }))
            if (api.succeeded) {
                toggleUnreadNotifications(false)
            }
        }
        
        async function goto(item) {
            item.read = true
            await client.api(new MarkAsRead({ notificationIds:[item.id] }))
            location.href = item.href
        }
        
        return { transition1, hide, filteredResults, filter, typeLabel, formatDate, goto, markAll }
    }
}

const AchievementsMenu = {
    template: `
<div v-if="!hide" :class="[transition1,'absolute top-12 right-0 z-10 mt-1 origin-top-right rounded-md bg-white dark:bg-gray-800 shadow-lg ring-1 ring-black ring-opacity-5 focus:outline-none w-[26rem] sm:w-[30rem]']" role="menu" aria-orientation="vertical" aria-labelledby="menu-button" tabindex="-1">
    <div class="py-1 px-2 bg-gray-50 dark:bg-gray-900 flex justify-between text-sm items-center border-b border-gray-200 dark:border-gray-700">
        <span class="py-1">
            achievements
        </span>
    </div>
  <div class="max-h-[20rem] overflow-auto" role="none">
    <ul>
        <li v-for="entry in filteredResults" :key="entry.title">
            <div class="py-2 px-2 text-sm flex justify-between font-semibold border-b border-gray-300 dark:border-gray-600">
                <span class="">{{entry.title}}</span>
            </div>
            <div v-for="item in entry.results" class="pr-2 py-2 hover:bg-indigo-100 dark:hover:bg-indigo-800 cursor-pointer border-b border-gray-200 dark:border-gray-700" @click="goto(item)">
                <b v-if="item.score > 0" class="mr-2 text-sm inline-block w-10 text-right text-green-600">+{{item.score}}</b>
                <b v-else-if="item.score < 0" class="mr-2 inline-block w-10 text-right text-red-600">-{{item.score}}</b>
                <span class="text-xs font-normal truncate" :title="item.title">{{item.title}}</span>
            </div>
        </li>
        <li v-if="!filteredResults.length">
            <div class="px-2 py-2 text-xs font-normal text-gray-500">empty</div>
        </li>
    </ul>
  </div>
</div>  
  `,
    setup(props) {
        const client = useClient()
        const show = ref(false)
        const results = ref([])
        const hide = ref(true)

        const filteredResults = computed(() => {
            const to = []
            const sevenDaysAgo = new Date() - 7 * 24 * 60 * 60 * 1000
            const last7days = results.value.filter(x => new Date(x.createdDate) >= sevenDaysAgo)
            if (last7days.length > 0) {
                to.push({ title: 'Last 7 days', results: last7days })
            }
            const thirtyDaysAgo = new Date() - 30 * 24 * 60 * 60 * 1000
            const last30days = results.value.filter(x => new Date(x.createdDate) >= thirtyDaysAgo && !last7days.includes(x))
            if (last30days.length > 0) {
                to.push({ title: 'Last 30 days', results: last30days })
            }
            const title = last7days.length + last30days.length === 0 ? 'All time' : 'Older'
            const remaining = results.value.filter(x => !last7days.includes(x) && !last30days.includes(x))
            if (remaining.length > 0) {
                to.push({ title, results: remaining })
            }
            return to
        })

        const transition1 = ref('transform opacity-0 scale-95')
        let timeout = null
        bus.subscribe('toggleAchievements', () => {
            clearTimeout(timeout)
            hide.value = false
            show.value = !show.value
            if (!show.value) timeout = setTimeout(() => hide.value = true, 700)
        })
        bus.subscribe('hideAchievements', () => {
            show.value = false
            hide.value = true
        })
        watch(show, () => {
            transition(rule1, transition1, show.value)
        })
        
        async function updateAchievements() {
            const api = await client.api(new GetLatestAchievements())
            if (api.succeeded) {
                results.value = api.response.results || []
                toggleUnreadAchievements(api.response.hasUnread)
            }
        }

        onMounted(async () => {
            await updateAchievements()
        })

        async function goto(item) {
            location.href = item.href
        }

        return { transition1, hide, filteredResults, formatDate, goto }
    }
}

function toggleUnreadNotifications(hasUnread) {
    const alert = $1('#new-notifications')
    if (!alert) return
    alert.classList.toggle('text-red-500', hasUnread)
    alert.classList.toggle('text-transparent', !hasUnread)
}
function toggleUnreadAchievements(hasUnread) {
    const alert = $1('#new-achievements')
    if (!alert) return
    alert.classList.toggle('text-red-500', hasUnread)
    alert.classList.toggle('text-transparent', !hasUnread)
}

function toggleNotifications(el) {
    bus.publish('toggleNotifications')
    bus.publish('hideAchievements')
}
function toggleAchievements(el) {
    bus.publish('toggleAchievements')
    bus.publish('hideNotifications')
    toggleUnreadAchievements(false)
}

function bindGlobals() {
    globalThis.toggleUnreadNotifications = toggleUnreadNotifications
    globalThis.toggleUnreadAchievements = toggleUnreadAchievements
    globalThis.toggleNotifications = toggleNotifications
    globalThis.toggleAchievements = toggleAchievements
}

const svg = {
    clipboard: `<svg class="w-6 h-6" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><g fill="none"><path d="M8 5H6a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2v-1M8 5a2 2 0 0 0 2 2h2a2 2 0 0 0 2-2M8 5a2 2 0 0 1 2-2h2a2 2 0 0 1 2 2m0 0h2a2 2 0 0 1 2 2v3m2 4H10m0 0l3-3m-3 3l3 3" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"></path></g></svg>`,
    check: `<svg class="w-6 h-6 text-green-500" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path></svg>`,
}

globalThis.copyBlock = function copyBlock(btn) {
    console.log('copyBlock',btn)
    const label = btn.previousElementSibling
    const code = btn.parentElement.nextElementSibling
    label.classList.remove('hidden')
    label.innerHTML = 'copied'
    btn.classList.add('border-gray-600', 'bg-gray-700')
    btn.classList.remove('border-gray-700')
    btn.innerHTML = svg.check
    navigator.clipboard.writeText(code.innerText)
    setTimeout(() => {
        label.classList.add('hidden')
        label.innerHTML = ''
        btn.innerHTML = svg.clipboard
        btn.classList.remove('border-gray-600', 'bg-gray-700')
        btn.classList.add('border-gray-700')
    }, 2000)
}

export function addCopyButtonToCodeBlocks() {
    console.log('addCopyButtonToCodeBlocks')
    $$('.prose pre>code').forEach(code => {
        let pre = code.parentElement;
        if (pre.classList.contains('group')) return
        pre.classList.add('relative', 'group')

        const div = createElement('div', { attrs: { className: 'opacity-0 group-hover:opacity-100 transition-opacity duration-100 flex absolute right-2 -mt-1 select-none' } })
        const label = createElement('div', { attrs: { className:'hidden font-sans p-1 px-2 mr-1 rounded-md border border-gray-600 bg-gray-700 text-gray-400' } })
        const btn = createElement('button', { 
            attrs: { 
                className:'p-1 rounded-md border block text-gray-500 hover:text-gray-400 border-gray-700 hover:border-gray-600',
                onclick: 'copyBlock(this)'
            } 
        })
        btn.innerHTML = svg.clipboard
        div.appendChild(label)
        div.appendChild(btn)
        pre.insertBefore(div,code)
    })
}

export default {
    load() {
        console.log('header loaded')
        bindGlobals()
        addCopyButtonToCodeBlocks()

        forceMount($1('#notifications-menu'), NotificationsMenu)
        forceMount($1('#achievements-menu'), AchievementsMenu)
    }
}
