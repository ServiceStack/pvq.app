import { $$, $1, on, JsonServiceClient } from "@servicestack/client"
import { useAuth, useClient } from "@servicestack/vue"
import { UserPostData, PostVote } from "dtos.mjs"

const { user } = useAuth()
const userName = user.value?.userName

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

const client = new JsonServiceClient()
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

const postId = parseInt($1('[data-postid]')?.getAttribute('data-postid'))
if (!isNaN(postId)) {
    const api = await client.api(new UserPostData({ postId }))
    if (api.succeeded) {
        origPostValues = api.response
        userPostVotes = Object.assign({}, origPostValues)
        console.log('origPostValues', origPostValues)
        $$('.voting').forEach(updateVote)
    }
}
