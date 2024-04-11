import ChartJs from "../components/ChartJs.mjs"

const colors = {
    "Gemma 7B": "#1d4ed8",
    "Gemma 2B": "#60a5fa",
    "Mistral 7B": "#b91c1c",
    "Code Llama 7B": "#0ea5e9",
    "DeepSeek Coder 6.7B": "#4f46e5",
    "Phi-2 2.7B": "#c026d3",
    "Qwen 1.5 4B": "#581c87"
}

const avatarMapping = {
    "Gemma 7B": "/avatar/gemma",
    "Gemma 2B": "/avatar/gemma-2b",
    "Mistral 7B": "/avatar/mistral",
    "Code Llama 7B": "/avatar/codellama",
    "DeepSeek Coder 6.7B": "/avatar/deepseek-coder",
    "Phi-2 2.7B": "/avatar/phi",
    "Qwen 1.5 4B": "/avatar/qwen-4b"
}

export default {
    components: { ChartJs },
    template: `
        <ChartJs :data="data" :plugins="plugins" />
    `,
    props:['results'],
    setup(props) {
        let results = props.results
        results.sort((a,b) => b.value - a.value)
        results = results.slice(0,10)
        
        const dataset = {
            label: 'Win Rates',
            data: results.map(x => x.value),
            avatars: results.map(x => avatarMapping[x.displayName]),
            backgroundColor: results.map(x => colors[x.displayName] + '7F'),
            borderColor: results.map(x => colors[x.displayName]),
            borderWidth: 1
        }

        const data = {
            labels: results.map(x => x.displayName),
            datasets: [dataset]
        }

        const plugins = [{
            id: 'barAvatar',
            afterDatasetDraw: (chart,args,options) => {
                const {ctx,chartArea: {top, bottom, left, right, width, height},
                    scales: {x, y}} = chart;
                let datasets = chart.data.datasets;
                ctx.save();

                const avatarSize = 42
                
                results.forEach((r,i) => {
                    const avatar = new Image()
                    avatar.src = datasets[0].avatars[i]
                    const dataValue = datasets[0].data[i]
                    let yPadding = 5
                    let yLoc = y.getPixelForValue(dataValue) - (avatarSize + yPadding)
                    yLoc = Math.max(top - avatarSize/2, yLoc)
                    ctx.drawImage(avatar, x.getPixelForValue(i) - (avatarSize/2), yLoc, avatarSize, avatarSize);
                });
            }

        }]

        return { data, plugins }
    }
}
