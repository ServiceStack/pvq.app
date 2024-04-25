---
title: Analyzing Voting Methods
summary: Analyzing the performance of different models for voting on individual answer vs on a group of answers.
author: Darren Reid
tags: [ai, llm, moderation]
image: https://images.unsplash.com/photo-1523961131990-5ea7c61b2107?crop=entropy&fit=crop&h=1000&w=2000
---

During the development of PvQ, we have generated over **1 million** answers from a variety of models to get a base line of 
performance per model by sampling, and ranking those answers as described in a previous blog post. While the technique 
of ranking via votes was imperfect, we found that on average it provided a reasonably representative sample to compare 
the general performance of each model relative to the others. Since that initial run, we improved the process to let a 
relatively larger model, [Mixtral 8x7B](https://mistral.ai/news/mixtral-of-experts/), to give a **vote out of 10** for each answer individually.

### Summary

- **Grouped voting** was more efficient by tokens, but less consistent, especially as more models were added.
- **Individual voting** was more consistent, and gave a better representation of the model's performance, and insights into the model voting.
- **Smaller answer models** like [Qwen 4B](https://ollama.com/library/qwen:4b) were more accurately ranked by higher performing models like [GPT 3.5 Turbo](https://platform.openai.com/docs/models/gpt-3-5-turbo), and [Claude 3 Sonnet](https://www.anthropic.com/news/claude-3-family).
- **Smaller ranking models** like [Claude 3 Haiku](https://www.anthropic.com/news/claude-3-family) usually agreed with larger models, but likely through proxies like length, formatting, etc.
- [Gemma 2B](https://blog.google/technology/developers/gemma-open-models/) was a stand out for its size, consistently producing well formatted answers, and so far never producing invalid or bad output.
- [Gemini Pro 1.0](https://blog.google/technology/ai/google-gemini-ai/) is fast, but too generous with votes, and likely influenced by length, formatting, etc, or needs to be prompted very differently.
- Some models would get into loops and output repeating tokens or phrases causing wasted API calls or GPU time.
- We are working on a classifier to detect these issues, and removing them automatically in an efficient way.

See below for a more detailed breakdown of the results.

## What we found

By analysing and voting on answer individually, we traded off an increased cost, we an increased accuracy of the voting.
Voting as a group of answers was more efficient by tokens, but as we added more models, the voting became less consistent
which we think highlighted the model struggling with the larger context window of input. By voting of answers individually,
the input size remains steady since it only consistent of the question, the answer being voted on, and instructions for the vote.

## Optimising Output

With this updated approach, we were duplicating the question tokens for each model that answers a question. The output originally asked the model to first analyse the answer in a chain-of-though way, before proceeding to then serialise this `reason` and `score` into JSON. This approach had a few problems.

- Sometimes the JSON with the score would be given first, negating any advantage in using a CoT approach since the additional tokens generated up front had no impact on the score. 
- The reason was somewhat duplicated.

Each token can significantly impact compute cost. Therefore, we wanted to reduce the token usage while still maintaining decent performance. When the instruction was given to output a specific JSON format, we noticed that the order of the properties in the flat output was consistently followed, eg:

```json
{
    "score": 5,
    "reason": "This was a good answer, but could have been improved with more code examples."
}
```

This was because the instruction included examples with `score` and `reason` in this order. So an optimization that proved to be more consistent was to give the examples with `reason` first, and `score` second. This ensured a shorter, mostly JSON only response, as well as the model generating the rational first. And this was done with very simple templating.

```js
const expectedReasonsSchema = {
    "reason": "Your reason goes here. Below score is only an example. Score should reflect the review of the answer.",
    "score": 1
}

content += `
## Example JSON Response

\`\`\`json
${JSON.stringify(expectedReasonsSchema, null, 4)}
\`\`\`

Use code fences, aka triple backticks, to encapsulate your JSON object.
`
```

This increased our throughput by 2-3x, with the same reduction in token usage without any noticeable impact on the quality of the votes.

## Testing Various Models for Voting

By default, we had been using Mixtral 8x7B as the model of choice to vote on the answers. There were several reasons for this choice:

- It was an open weights model we could run anywhere
- It performed the task quite well
- It was fast thanks to its MoE (Mixture of Experts) design outputting at speeds common for 12-13B parameter models

However, as we added more high performing models for answers, we realised that voting is only as good as the model doing the votes. Eg, how correct or useful an answer was required the model to detect more nuanced errors. And while Mixtral did identify even errors from its own generation, we could see in the distribution of the votes, this was still limited.

To get more evidence for this, and to speed up the re-processing of missed voting, we decided to split up the remaining 400-500k answer votes needed among different models both open weight, and proprietary. We used:

- Mixtral 8x7B
- GPT 3.5 Turbo
- Claude Sonnet
- Claude Haiku
- Gemini Pro 1.0
- Llama 3 8B
- Command-R
- Llama 3 70b

By using Anthropic's Claude models for voting, this also enabled us to better evaluate their performance, along with GPT 3.5 Turbo as a well known performer, the new comer of Llama 3 8B, and Gemini Pro 1.0 for good measure. Something worth noting was while Gemini Pro 1.0 might not have been the best model for accurate voting, the speed of response was quite impressive, coming out as the fastest way to process answers out of this selection of models.

![graph](/img/posts/individual-voting-comparison/overall-votes-given.png)

| GradedBy              | Count  |
| --------------------- | ------ |
| Grouped Voting        | 540944 |
| Mixtral 8x7B          | 223039 |
| Gemini-Pro 1.0        | 136586 |
| GPT 3.5 Turbo         | 81015  |
| Claude 3 Haiku        | 72183  |
| Claude 3 Sonnet       | 24147  |
| Llama 3 8B            | 10765  |
| Command-R (By Cohere) | 997    |
| Llama 3 70B           | 522    |

## Performance Group vs Individual Voting

After collecting the votes, we can then look at how the same model was ranked and given votes by different reviewer models, including Mixtral in both individual and group voting approaches. Looking first at a smaller model, the change can be more easily seen.

#### Group Ranking Qwen-4B

![graph](/img/posts/individual-voting-comparison/group-qwen-4b-votes.png)

#### Individual Ranking Qwen-4B by Mixtral

![graph](/img/posts/individual-voting-comparison/mixtral-qwen-4b-votes.png)

For the group ranking, we get a split result, where as for the individual ranking, we see a more realistic bell curve appearing. While it is up for interpretation as to what this means, when looking at it with the rest of the data, it seems this is a sign of a more stable ranking process since the extremes are less likely to occur and for the case of Qwen 4B which is one of the smaller models used, it is on average not a very good performer. If we use a known higher performance model to do the ranking like GPT 3.5 Turbo, that curve is still present, with the extremes even less likely, and the average performance of the model even more obvious.

![graph](/img/posts/individual-voting-comparison/gpt-qwen-4b-votes.png)

Claude 3 Sonnet, which is also a relatively strong model shows a similar thing for Qwen 4B, but with a smaller sample size.

![graph](/img/posts/individual-voting-comparison/sonnet-qwen-4b-votes.png)

In contrast, Claude 3 Haiku exhibited more instability in its vote distribution, despite having a larger sample set.

![graph](/img/posts/individual-voting-comparison/haiku-qwen-4b-votes.png)

One conclusion that could be drawn is that there is a minimum capability of a model to perform the voting itself. Another could be that the complexity of the task being solved in the answer needs to be within the capability of the model to be more consistently, and accurately voted on.

## Strong Answer Model vs Weaker Ranking Model

Among the vote distribution by different models, we can see an interesting trend. For example, here is Claude Haiku vote distribution for Mixtral generated answers.

![graph](/img/posts/individual-voting-comparison/haiku-mixtral-votes.png)

For comparison, here is Claude 3 Sonnet, and GPT 3.5 Turbo

![graph](/img/posts/individual-voting-comparison/sonnet-mixtral-votes.png)

![graph](/img/posts/individual-voting-comparison/gpt-mixtral-votes.png)

So for answers generated by more capable models, Claude 3 Haiku seems to provide pretty consistent votes, similar to that of larger models. Yet, if we then compare the same three voting models for Qwen 4B as above, we get this divergence. This might suggest Claude 3 Haiku is instead using various proxies for voting like length, formatting, etc, which might be more inconsistently generated by models like Qwen 4B. Another piece of data that supports this is the votes Claude 3 Haiku gives to answers by Gemma-2B.

![graph](/img/posts/individual-voting-comparison/haiku-gemma-2b-votes.png)

Something we noticed looking at answers generated by Gemma-2B is that they are nearly always well formatted, with even use of Markdown tables in answers. While the accuracy of the info left a lot to be desired, it was one of the few models that never produced invalid or bad output when generating answers via Ollama. This was especially surprising considering the largest amount of errors came from the other two smallest models of Phi 2 and Qwen 4B. Again, comparing this vs GPT 3.5 Turbo, and we see a more accurate reflection of the quality of answers from Gemma-2B.

![graph](/img/posts/individual-voting-comparison/gpt-gemma-2b-votes.png)

The contrast is still not as clear as others, but for simple questions, Gemma-2B did punch above its weights (pun intended) in quality results, so this might point to the inconsistency of the model answers rather than inconsistencies in the vote distribution.

## Gemini Pro 1.0 Speed vs Performance

One of the models that did a lot of the heavy lifting when processing all these voting tasks was Google's Gemini-Pro 1.0. Their API was highly reliable, and surprisingly quick with responses usually between 1-3 seconds. However, Gemini-Pro 1.0 tended to be very generous with its scoring. Even when ranking Microsoft's Phi 2 which consistently produced low quality answers leading us to remove it from the models providing answers. Despite these low quality answers, lots of high votes were given to Phi 2 answers by Gemini Pro 1.0.

![graph](/img/posts/individual-voting-comparison/gemini-phi-votes.png)

And according to Gemini-Pro 1.0, Mixtral was almost incapable of making a mistake. 

![graph](/img/posts/individual-voting-comparison/gemini-mixtral-votes.png)

And even Gemma-2B wasn't far off for Gemini-Pro 1.0.

![graph](/img/posts/individual-voting-comparison/gemini-gemma-2b-votes.png)

While Mixtral did provide decent answers on the whole, it was not without its faults and issues, as seen above from the more representative votes from GPT 3.5 Turbo for Mixtral answers.

So while there is some signal to votes from Gemini Pro 1.0, it is much harder to see, and seems more likely that the length, formatting and other metrics influenced the voting more so than the other models giving votes.

The same prompt was used for all voting, so it is also possible that with different prompting, Gemini-Pro 1.0 might be more useful at this kind of task.

## Extracting the Votes

Another challenge of utilising LLMs and other probabilistic systems is that you need to always expect to get strange results sometimes. This leads to the problem of not wanting to waste tokens, but also being strict with what output should be expected. By only expecting a JSON output in a code fence, you would need to throw away minor variations in this that still contain a valid voting result.

Instead, to reduce wasted generations, we need to employ several attempts to extract that JSON, while testing it is indeed what we need for the rest of our system to function. During testing we found output from various models would differ, causing a strict extraction via a single regex would be insufficient. Since the output was inconsistent, our solution also lacked consistency. The result was a somewhat hacky approach to cover multiple irregular outputs with as few methods as possible.

If we only used open weight models, there are tricks like limiting the model vocabulary during output generation to ensure valid JSON which provides certain guarantees for consistent generation, and an option like this added to tools like llama.cpp or Ollama would a great addition.

## Error Rates and Closing Thoughts

Voting on each and every answer is quite compute intensive, but it does give us valuable signal to the quality of some of the answers given, even if formatting, length, etc are over represented. However, a common enough problem we saw was some models would get into loops and output repeating tokens or phrases causing us to waste API calls or GPU time to ranking answers that should be outright filtered. Below is an example of the kind of invalid content generation we can see from models.


> With the Win32SharedLibrary interface and using .NET's DLLImporter class. It's pretty simple. Here is an 
example of such an interface which works as .NET DllImports are used to a .C.D. for Windows 10. You can use 
this cpp to build your cpp from the Win32. SharedD. A.B.D. for Windows. and in the Installer/Ming W. C. A.B.B.
D.A. A. B.C. D.B.B.B.D. The M. A. A. A. A. A. A.A. A. B. B.B.B.B.B. B.B.B. B.B.B. B.B.B. B.B.B.B. B. B. B.B. 
B.B.B. B. B. B. A. C. A.A. B. A. B.A. B.A. B.A. B.A. B. A. B. A. B. A. A. A. A.A. A. A. A. A. A. A. B. B.B. 
B.B.B. B.B. B.B. B.B.B. B.B.B. B.B.B.B.B.B. B. B. A. A.A.A. I'm sorry. My apologies but my and aa/a/a/I can't, 
you/I/do. Do this myself because it's/isn't as simple. You don't have to go yourself to a c.c.c.c.c.a.b.c.a.
B.c.a.A. A.A.B.B.B.B.b. B.b.b.b. b.a. b. A. \n\n

Making it worse, these outputs tended to happen when the model was unable to output the stop token, causing generation to hit the `max_tokens` limit provided to the API host, causing the maximum amount of tokens used, which were then fed into the ranking system. 

Just like extracting JSON, detecting and filtering this was quite cumbersome, and using another LLM to detect these issues doesn't reduce our compute usage by much. To get a better picture of the data in the generated answers, we build a script to extract metadata of the generated text like `Length`,`MaxCharCount`,`MaxWordCount`, and others. And patterns started to stick out enough to filter most of these data out, but it still was time intensive to find practical thresholds to detect bad data while keeping all the answers that were at least somewhat coherent. After doing this mostly manually to start, we realized that pattern recognition is exactly what simple neural nets or 'Narrow AI' is good at, and we have training data to build a small efficient classifier that only looks at the metadata of the answer to match patterns.

We've spiked our first implementation of such a model, and while initial results are promising, more testing still needs to be done to ensure it will be useful enough to further reduce the amount of compute required to avoid invalid answer generation from reaching the site. Currently the model is just 23MB in size and is showing more than 99.9% accuracy with a realistic mixture of valid and invalid data, and 91% when given all bad data, both evaluations used data excluded from the original training set. 

## Feedback ❤️

We're still in the very early stages of development and would love to hear your feedback on how we can improve pvq.app to become a better platform for answering technical questions. You can provide feedback in our [GitHub Discussions](https://github.com/ServiceStack/pvq/discussions):

- [Feature Requests](https://github.com/ServiceStack/pvq/discussions/categories/ideas)
- [Report Issues](https://github.com/ServiceStack/pvq/issues)
- [General Feedback](https://github.com/ServiceStack/pvq/discussions)