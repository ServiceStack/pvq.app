window.hljs?.configure({ignoreUnescapedHTML:true})
window.hljs?.highlightAll()

function loadTags() {
    if (!window.assetsUrl) return
    fetch(window.assetsUrl('/data/tags.txt'))
        .then(r => r.text())
        .then(txt => localStorage.setItem('data:tags.txt', txt.replace(/\r\n/g,'\n')))
}

if (!localStorage.getItem('data:tags.txt'))
{
    loadTags()
}

function metadataDate(metadataJson) {
    try {
        if (metadataJson) {
            return new Date(parseInt(metadataJson.match(/Date\((\d+)\)/)[1]))
        }
    } catch{}
    return new Date() - (24 * 60 * 60 * 1000) 
}

const metadataJson = localStorage.getItem('/metadata/app.json')
const oneHourAgo = new Date() - 60 * 60 * 1000
const clearMetadata = !metadataJson
    || location.search.includes('clear=metadata')
    || metadataDate(metadataJson) < oneHourAgo 

if (clearMetadata) {
    fetch('/metadata/app.json')
        .then(r => r.text())
        .then(json => {
            console.log('updating /metadata/app.json...')
            localStorage.setItem('/metadata/app.json', json)
        })
}

// highlight the element with the given id
function highlightElement(id) {
    const el = document.getElementById(id)
    if (el) {
        el.classList.add('highlighted')
        el.scrollIntoView('smooth')
    }
}

if (location.hash) {
    highlightElement(location.hash.substring(1))
}

document.addEventListener('DOMContentLoaded', () => {
    Blazor.addEventListener('enhancedload', (e) => {
        if (location.hash) {
            highlightElement(location.hash.substring(1))
        }
    })
    loadTags()
})
