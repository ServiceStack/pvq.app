import { useClient, useAuth } from "@servicestack/vue"
import { UpdateUserProfile } from "dtos.mjs"

export default {
    template:`<form @submit.prevent="submit" class="mt-8">
        <div class="px-4 sm:px-6">
            <FileInput id="profileUrl" label="Avatar" v-model="user.profileUrl" />
        </div>
        
        <div class="mt-4 bg-gray-50 dark:bg-gray-800 px-4 py-3 text-right sm:px-12">
            <div class="flex justify-between space-x-3">
                <div></div>
                <div>
                    <PrimaryButton :disabled="!client.loading">Save</PrimaryButton>
                </div>
            </div>
        </div>        
    </form>`,
    
    setup() {
        const client = useClient()
        const { user } = useAuth()
        
        user.value.profileUrl = `/avatar/${user.value.userName}?t=${new Date().getTime()}`
        
        async function submit(e) {
            const api = await client.apiForm(new UpdateUserProfile(), new FormData(e.target))
            if (api.succeeded) {
                user.value.profileUrl = `/avatar/${user.value.userName}?t=${new Date().getTime()}`
                const el = document.querySelector('#user-avatar')
                if (el) el.src = user.value.profileUrl 
            }
        }
        return { user, submit, client }
    }
}
