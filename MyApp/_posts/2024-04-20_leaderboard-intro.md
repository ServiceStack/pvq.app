---
title: Generating the PvQ Leaderboard
summary: Analyzing Questions and Answers Using Open Source LLMs to evaluate the quality of answers.
author: Darren Reid
tags: [ai, llm, docs]
image: https://images.unsplash.com/photo-1579407364450-481fe19dbfaa?crop=entropy&fit=crop&h=1000&w=2000
---

Recently, we've been building a small application called PvQ, a question and answer site driven by open weight large-language-models (LLMs). We started with ~100k questions from the StackOverflow dataset, and had an initial set of 7 open weight LLMs to produce an answer using a simple zero shot prompt. We needed a way to see the site with useful rankings to help push the better answers two the top without us manually reviewing each answer. While it is far from an perfect approach, we decided to use the Mixtral model from Mistral.AI, to review the answers together, and vote on the quality in regards to the original question.

In this post I will go over the results we found as well as talk about why sites like PvQ, StackOverflow and others are actually a better interface than instant response chat bots when it comes to seeking assistance from LLM powered models and agents.

## Generating the Answers

One of the advantages of open weight models is the ability to run them on your own hardware, which can lead to significant cost reductions vs hosted solutions. Using a MacBook Pro M2 and a workstation with 3 Nvidia A4000 GPUs, we were able to both generate the answers for these models as well as rank them with the addition of two more A4000s to speed up the process. Even when Mistral.AI offers very cost competitive access to their Mixtral model at $0.7 per 1 million tokens (in and out), it can still be 10-20x cheaper to run on your own hardware if you just count the cost of power, which admittedly can vary wildly by your location. Over a few weeks we generated ~700k answers for the following models:

- Mistral 7B Instruct
- Gemma 7B Instruct
- Gemma 2B Instruct
- Deepseek-Coder 6.7B
- Codellama
- Phi 2.0
- Qwen 1.5 4b

We stuck to models that are small enough to run and are accessible via the [Ollama Library of models](https://ollama.com/library) for ease of use. The speed of generation also varied quite a bit for this group of models. Phi 2.0 for example, despite being having fewer parameters than other models, was one that took longer than others to finish, partly because the answers were quite long on average. Codellama and Deepseek-Coder, and Qwen 1.5 4B had the fastest inference, followed closely by Google's Gemma models.

By having answers for all these models in one place for the same question, it can be useful to show the relative performance of specific tasks if you are evaluating which model to use for similar problems. The async nature of QA sites like **PvQ also lends itself to having a more in depth process in the future** for producing answers of higher quality by using common LLM techniques like **multi-agent workflows, Retrieval Augmented Generation, tool use and more**. And lastly, the questions and answers are **public, discoverable content by default, which lends itself to greater reuse of the compute expended to generate the answers** rather than making it mostly ephemeral in a chat interface.

## Model Performance

Once our answers were generated, we used the larger Mixtral-8x7B model to evaluate the answers and assign votes out of 10. We tried to do this smaller models, as well as some commercial offerings for comparison, and reviewing answers for errors or issues is definitely a harder tasks for LLMs than generating the answers themselves. We found that while Mixtral wasn't perfect, it still provided value to identify outliers of particularly poor answers, and was consistent with models that produced plausible answers. And this is the common weakness in using LLMs for such tasks, they can sound very plausible while still getting details wrong. And the reviewing process was the same story. 

To try to remove the biases we could, answers were randomly sorted and assigned single letter names using the format `Answer A` for example. The Mixtral reviewer was then asked to give 0-10 votes on each answer, and to justify their answer before voting. Mixtral was able to follow the instructions well, though sometimes votes were given upfront, before critiquing each answer. By forcing the model to critique the model first, additional tokens related to each answer are generated, giving the model a higher chance of an appropriate vote, or "longer to think" about what vote should be given to each answer. 

We then extract the reasoning and the score for future use in our system including ranking, and additional metadata about the answer.

```
{
    "gemma": {
        "score": 10,
        "reason": "This answer is high quality and relevant to the user's question. It provides a clear and concise code example using the System.Diagnostics class to get the total amount of RAM. The additional notes provide a good explanation of the code example."
    },
    "deepseek-coder-6.7b": {
        "score": 10,
        "reason": "This answer is high quality and relevant to the user's question. It provides a clear and concise code example using the ManagementObjectSearcher class to get the total amount of RAM. The additional notes provide a good explanation of the code example."
    },
    "mistral": {
        "score": 8,
        "reason": "This answer is relevant and high quality. It suggests using the Microsoft.Management.Instrumentation package to get the total amount of RAM. However, it assumes that the user is familiar with this package and does not provide a clear code example."
    },
}
```

This is done for all ~100k questions and answer sets which enables us to get an LLM generated snapshot of the distribution of answers and their 'quality' via voting and justification (reason) for that score (number of votes).

![](https://raw.githubusercontent.com/mythz/pvq.app/main/MyApp/wwwroot/img/posts/leaderboard/total-votes.png)

Here we have a chart of total number of votes assigned by model. These are some intermediate values, but you can see the smaller, less capable models are trending towards the lower end while the larger models are getting a larger proportion of the votes.

We also have Win Rates, which is how often the each model produces the highest or equal highest score for a question it gave an answer.

![](https://raw.githubusercontent.com/mythz/pvq.app/main/MyApp/wwwroot/img/posts/leaderboard/win-rates.png)

Again, we can see a disproportionate win rate for larger models. Gemma 7B and Mistral 7B are quite close, but this might be due to better answers or something the Mixtral model (who did the voting) uses as a proxy for quality, something we will need to look more closely at in the future. Manual ranking shows that in general, the Gemma 7B produces decent answers for a lot of questions, but something striking is that it also produces very well laid out answers, consistently making good use of Markdown tables, well labelled headings and the like.

![](https://raw.githubusercontent.com/mythz/pvq.app/main/MyApp/wwwroot/img/posts/leaderboard/gemma-7b-example.png)

This is just an initial attempt to help better organise the data generated, and do so utilising automation to scale to a large content base. While there are some limitations, it does show it can be useful and we are looking at improving the process to give a better signal to noise on PvQ answers while still showing all model responses which can be used to help others evaluate what tasks might best suit specific models. We also added a feature to check performance by tags as well.

## Performance by Tags

Since the original StackOverflow questions are tagged, we can also then have a look at the voting distribution by tag which commonly relate to a classification of the topic of the question. Some tags have a lot more questions than others, and with popular tags, we get a larger sample size to try to evaluate if models have strengths or weaknesses in particular topics.

For example, if we compare the overall Win Rate chart, where a model had the highest or equal highest score across all questions, we can see the following ranking.

![](https://raw.githubusercontent.com/mythz/pvq.app/main/MyApp/wwwroot/img/posts/leaderboard/win-rates.png)

If we then show the overall win rate side by side with the win rate on just questions tagged with `c#` for example, we see about the same performance by each model.

![](https://raw.githubusercontent.com/mythz/pvq.app/main/MyApp/wwwroot/img/posts/leaderboard/win-rates-compare-csharp.png)

Where as if we have a look at win rates for questions tagged with `python`, we can also seem some larger movements in the results.

![](https://raw.githubusercontent.com/mythz/pvq.app/main/MyApp/wwwroot/img/posts/leaderboard/win-rates-compare-python.png)

Deepseek-Coder 6.7B gets a small bump in Python performance (right hand side bar), and Phi has a much larger jump from 12% to 18.15% wins for every question it was ranked in.

A daily updated version of the leaderboard can be found at [pvq.app/leaderboard](https://pvq.app/leaderboard).

## Ways to improve

Currently our LLM powered voting process is very simple. A single large prompt using Mixtral 8x7B to critique each answer based on the question, and provide a score to reflect that critique. Batching all these into one prompt won't scale well, but thankfully voting out of 10 can be done per answer with the trade off of more prompts to the model, and duplicate input tokens. 

## Chatbots vs Async Forums

Answers are also done using a 'zero-shot' method with a simple prompt that includes the question. And this is where the async nature of sites like PvQ I think have a lot of room to improve the quality of the data coming back. Workflows can be produced with additional quality control steps before answers are posted. The models can make edits, or have multiple tries at a question before posting. While it lacks the instant feedback of chat bots, this seems to be an area which is wide open for significant improvements. And lots of techniques have shown how there is a lot of quality/accuracy/performance still remaining to extract from LLMs if processes like [SmartGPT](https://youtu.be/wVzuvf9D9BU?si=MoVaPwsuz-U0WoyQ), and other [agentic workflows](https://youtu.be/sal78ACtGTc?si=8qqucMDdj7laKcLm).

## Sharing Answers

The other major advantage is the effort and compute put into these systems to create and refine answers to common questions can be more easily discovered by traditional search. So even if the process uses additional compute resources to produce, it is computationally cheap to find and reuse.

## More Powerful Models

As a way to encourage users to ask good questions, PvQ grants access to responses from larger open weight models like Mixtral, and possibly more in the future as we evaluate candidates like Command-R, and many more. As your account asks more questions you can get access to proprietary models:

- [Claude 3 Haiku](https://www.anthropic.com/news/claude-3-haiku) by Anthropic
- [Gemini Pro](https://blog.google/technology/ai/google-gemini-ai/) by Google
- [Claude 3 Sonnet](https://www.anthropic.com/news/claude-3-family) by Anthropic
- [GPT 4 Turbo](https://platform.openai.com/docs/models/gpt-4-and-gpt-4-turbo) by OpenAI
- [Claude 3 Opus](https://www.anthropic.com/claude) by Anthropic
- More to come

## Feedback ❤️

We're still in the very early stages of development and would love to hear your feedback on how we can improve pvq.app
to become a better platform for answering technical questions. You can provide feedback in our
[GitHub Discussions](https://github.com/ServiceStack/pvq/discussions):

- [Feature Requests](https://github.com/ServiceStack/pvq/discussions/categories/ideas)
- [Report Issues](https://github.com/ServiceStack/pvq/issues)
- [General Feedback](https://github.com/ServiceStack/pvq/discussions)

