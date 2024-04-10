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
}

const modelAliases = {
    "gemma": "Gemini",
    "gemma-2b": "Gemini 2B",
    "mistral": "Mistral",
    "gpt-4-turbo": "GPT-4 Turbo",
    "claude-3-opus": "Claude 3 Opus",
    "claude-3-haiku": "Claude 3 Haiku",
    "claude-3-sonnet": "Claude 3 Sonnet",
}
export default {
    async load() {
        const el = $1("#winrate-chart")
        if (!el) return

        // Get data prop values from data attributes
        const datasetsArray = el.getAttribute("data-datasets")
        const optionsObj = el.getAttribute("data-options")

        // Get type prop value from data attribute
        const type = "bar"

        // Parse data attributes
        const datasets = JSON.parse(datasetsArray)
        const modelNames = datasets[0].modelNames

        // Create labels and data arrays based on the original order of modelNames
        const labels = modelNames.map(modelName => modelAliases[modelName] || modelName);
        const data = modelNames.map(modelName => datasets[0].data[modelNames.indexOf(modelName)]);

        // Create the dataset object
        const newDataset = {
            label: "Win Rate %",
            data: data,
            backgroundColor: modelNames.map(modelName => modelColors[modelName] || "#000"),
            borderColor: modelNames.map(modelName => modelColors[modelName] || "#000"),
            borderWidth: 1
        };

        const options = JSON.parse(optionsObj)

        mount(el, Chart, { type: type, data: { labels: labels, datasets: [newDataset] }})
    }
}