import { ref, computed, watch, onMounted } from "vue"
import { forceMount } from "app.mjs"
import { $1, toDate, EventBus } from "@servicestack/client"
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
<div v-if="!hide" :class="[transition1,'absolute top-12 right-0 z-10 mt-1 origin-top-right rounded-md bg-white dark:bg-gray-800 shadow-lg ring-1 ring-black ring-opacity-5 focus:outline-none w-[30rem]']" role="menu" aria-orientation="vertical" aria-labelledby="menu-button" tabindex="-1">
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
        <li v-for="item in filteredResults" :key="item.id" :class="['px-2 py-2 text-xs font-normal hover:bg-indigo-100 dark:hover:bg-indigo-800 cursor-pointer border-b border-gray-200 dark:border-gray-700', item.read ? 'bg-gray-100 dark:bg-gray-700' : '']" @click="goto(item)">
            <div class="flex justify-between font-semibold text-gray-500">
                <span class="">{{typeLabel(item.type)}}</span>
                <span>{{formatDate(item.createdDate)}}</span>
            </div>
            <div class="px-2 mt-1 truncate" :title="item.title"><b class="mr-1">Q</b>{{item.title}}</div>
            <div class="px-2 mt-2" :title="item.summary">{{item.summary}}</div>
        </li>
        <li v-if="!filteredResults.length">
            <div class="px-2 py-2 text-xs font-normal text-gray-500">No notifications</div>
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
        watch(show, () => {
            transition(rule1, transition1, show.value)
        })

        onMounted(async () => {
            const api = await client.api(new GetLatestNotifications())
            if (api.succeeded) {
                results.value = api.response.results || []
            }
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
            await client.api(new MarkAsRead({ allNotifications: true }))
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
<div :class="[show ? '' : 'hidden','absolute top-12 right-0 z-10 mt-1 w-56 origin-top-right rounded-md bg-white shadow-lg ring-1 ring-black ring-opacity-5 focus:outline-none']" role="menu" aria-orientation="vertical" aria-labelledby="menu-button" tabindex="-1">
  <div class="py-1" role="none">
    <!-- Active: "bg-gray-100 text-gray-900", Not Active: "text-gray-700" -->
    <a href="#" class="text-gray-700 block px-4 py-2 text-sm" role="menuitem" tabindex="-1" id="menu-item-0">Account settings</a>
    <a href="#" class="text-gray-700 block px-4 py-2 text-sm" role="menuitem" tabindex="-1" id="menu-item-1">Support</a>
    <a href="#" class="text-gray-700 block px-4 py-2 text-sm" role="menuitem" tabindex="-1" id="menu-item-2">License</a>
    <form method="POST" action="#" role="none">
      <button type="submit" class="text-gray-700 block w-full px-4 py-2 text-left text-sm" role="menuitem" tabindex="-1" id="menu-item-3">Sign out</button>
    </form>
  </div>
</div>  
  `,
    setup(props) {
        const show = ref(false)
        
        return { show }
    }
}


globalThis.toggleNotifications = function (el) {
    // console.log('toggleNotifications')
    bus.publish('toggleNotifications')
}

globalThis.toggleAchievements = function (el) {
    // console.log('toggleAchievements')
    bus.publish('toggleAchievements')
}

export default {
    load() {
        console.log('header loaded')
        const elNotificationsMenu = $1('#notifications-menu')
        const elAchievementsMenu = $1('#achievements-menu')

        forceMount(elNotificationsMenu, NotificationsMenu)
        forceMount(elAchievementsMenu, AchievementsMenu)
    }
}