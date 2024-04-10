import { ref, computed, watch, watchEffect, nextTick, onMounted, onUpdated, getCurrentInstance  } from "vue"
import { $$, $1, on, JsonServiceClient, EventBus, toDate, humanize, leftPart, lastRightPart } from "@servicestack/client"
import { useClient, useAuth, useUtils } from "@servicestack/vue"
import { mount, alreadyMounted, forceMount } from "app.mjs"
import {GetMeta} from "./dtos.mjs";
import Chart from "../posts/components/ChartJs.mjs";

const modelColors = {
    "gemma": "#1d4ed8",
    "gemma-2b": "#60a5fa",
    "mistral": "#b91c1c",
    "gpt-4-turbo": "#047857",
    "claude-3-opus": "#b45309",
    "claude-3-haiku": "#44403c",
    "claude-3-sonnet": "#78716c",
    "codellama": "#0ea5e9",
    "deepseek-coder-6.7b": "#4f46e5",
    "gemini-pro": "#8b5cf6",
    "phi": "#c026d3",
    "qwen-4b": "#581c87"
}

const modelAliases = {
    "gemma": "Gemini",
    "gemma-2b": "Gemini 2B",
    "mistral": "Mistral",
    "gpt-4-turbo": "GPT-4 Turbo",
    "claude-3-opus": "Claude 3 Opus",
    "claude-3-haiku": "Claude 3 Haiku",
    "claude-3-sonnet": "Claude 3 Sonnet",
    "codellama": "CodeLlama",
    "deepseek-coder-6.7b": "DeepSeek Coder 6.7b",
    "gemini-pro": "Gemini Pro",
    "phi": "Phi",
    "qwen-4b": "Qwen 1.5 4B"
}
export default {
    async load() {
        const el = $1("chart-js")
        if (!el) return
        
        // could be multiple charts on the page
        $$("chart-js").forEach(el => {
            // Get data prop values from data attributes
            const datasetsArray = el.getAttribute("data-datasets")
            const optionsObj = el.getAttribute("data-options")

            // Get type prop value from data attribute
            const type = "bar"

            console.log(datasetsArray);
            // Parse data attributes
            const datasets = JSON.parse(datasetsArray)
            const modelNames = datasets[0].modelNames

            // Create an object to map model names to their data and color
            const modelData = {};
            modelNames.forEach((modelName, index) => {
                modelData[modelName] = {
                    data: {
                        x: modelName,
                        y: datasets[0].data[index]
                    }
                };
            });

            // Create labels and datasets based on the original order of modelNames
            const labels = [];
            const newDatasets = [];

            modelNames.forEach(modelName => {
                labels.push(modelName);
            });
            
            newDatasets.push(
                {
                    data: modelNames.map(modelName => modelData[modelName].data),
                    label: datasets[0].label,
                    backgroundColor: modelNames.map(modelName => modelColors[modelName]),
                }
            )

            mount(el, Chart, { type: type, data: { datasets: newDatasets }})
        })
    }
}