window.hljs?.highlightAll()

if (!localStorage.getItem('data:tags.txt'))
{
    fetch('/data/tags.txt')
        .then(r => r.text())
        .then(txt => localStorage.setItem('data:tags.txt', txt.replace(/\r\n/g,'\n')));
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

document.addEventListener('DOMContentLoaded', () =>
    Blazor.addEventListener('enhancedload', (e) => {
        if (location.hash) {
            highlightElement(location.hash.substring(1))
        }
    }))
