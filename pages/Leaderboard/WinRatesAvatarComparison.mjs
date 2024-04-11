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
    "DeepSeek Coder 6.7B": "/avatar/deepseek-coder-6.7b",
    "Phi-2 2.7B": "/avatar/phi",
    "Qwen 1.5 4B": "/avatar/qwen-4b"
}

export default {
    components: { ChartJs },
    template: `
        <ChartJs :data="data" :plugins="plugins" />
    `,
    props:['results1','results2','tag'],
    setup(props) {
        let results = props.results1
        
        let results2 = props.results2
        let tag = props.tag
        const dataset = {
            label: 'Win Rates All vs',
            data: results.map(x => x.value),
            avatars: results.map(x => avatarMapping[x.displayName]),
            backgroundColor: results.map(x => colors[x.displayName] + '7F'),
            borderColor: results.map(x => colors[x.displayName]),
            borderWidth: 1
        }
        
        const dataset2 = {
            label: tag,
            data: results2.map(x => x.value),
            avatars: results.map(x => avatarMapping[x.displayName]),
            backgroundColor: results.map(x => colors[x.displayName] + '7F'),
            borderColor: results.map(x => colors[x.displayName]),
            borderWidth: 1
        }

        const data = {
            labels: results.map(x => x.displayName),
            datasets: [dataset,dataset2]
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
                    const dataValue = Math.max(datasets[0].data[i],datasets[1].data[i])

                    ctx.drawImage(avatar, x.getPixelForValue(i) - (avatarSize/2), y.getPixelForValue(dataValue) - (avatarSize*1.5), avatarSize, avatarSize);
                });
            }

        }]

        return { data, plugins }
    }
}
