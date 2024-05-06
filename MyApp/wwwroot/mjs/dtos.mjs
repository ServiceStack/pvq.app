/* Options:
Date: 2024-05-05 17:08:25
Version: 8.23
Tip: To override a DTO option, remove "//" prefix before updating
BaseUrl: https://localhost:5001

//AddServiceStackTypes: True
//AddDocAnnotations: True
//AddDescriptionAsComments: True
//IncludeTypes: 
//ExcludeTypes: 
//DefaultImports: 
*/

"use strict";
export class MailTo {
    /** @param {{email?:string,name?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    email;
    /** @type {string} */
    name;
}
export class EmailMessage {
    /** @param {{to?:MailTo[],cc?:MailTo[],bcc?:MailTo[],from?:MailTo,subject?:string,body?:string,bodyHtml?:string,bodyText?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {MailTo[]} */
    to;
    /** @type {MailTo[]} */
    cc;
    /** @type {MailTo[]} */
    bcc;
    /** @type {?MailTo} */
    from;
    /** @type {string} */
    subject;
    /** @type {?string} */
    body;
    /** @type {?string} */
    bodyHtml;
    /** @type {?string} */
    bodyText;
}
export class ResponseError {
    /** @param {{errorCode?:string,fieldName?:string,message?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    errorCode;
    /** @type {string} */
    fieldName;
    /** @type {string} */
    message;
    /** @type {{ [index: string]: string; }} */
    meta;
}
export class ResponseStatus {
    /** @param {{errorCode?:string,message?:string,stackTrace?:string,errors?:ResponseError[],meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    errorCode;
    /** @type {string} */
    message;
    /** @type {string} */
    stackTrace;
    /** @type {ResponseError[]} */
    errors;
    /** @type {{ [index: string]: string; }} */
    meta;
}
export class MailMessage {
    /** @param {{id?:number,email?:string,layout?:string,template?:string,renderer?:string,rendererArgs?:{ [index: string]: Object; },message?:EmailMessage,draft?:boolean,externalRef?:string,createdDate?:string,startedDate?:string,completedDate?:string,error?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    email;
    /** @type {?string} */
    layout;
    /** @type {?string} */
    template;
    /** @type {string} */
    renderer;
    /** @type {{ [index: string]: Object; }} */
    rendererArgs;
    /** @type {EmailMessage} */
    message;
    /** @type {boolean} */
    draft;
    /** @type {string} */
    externalRef;
    /** @type {string} */
    createdDate;
    /** @type {?string} */
    startedDate;
    /** @type {?string} */
    completedDate;
    /** @type {?ResponseStatus} */
    error;
}
export class SendMailMessages {
    /** @param {{ids?:number[],messages?:MailMessage[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?number[]} */
    ids;
    /** @type {?MailMessage[]} */
    messages;
}
export class CreateEmailBase {
    /** @param {{email?:string,firstName?:string,lastName?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    email;
    /** @type {string} */
    firstName;
    /** @type {string} */
    lastName;
}
/** @typedef {'Unknown'|'UI'|'Website'|'System'|'Migration'} */
export var Source;
(function (Source) {
    Source["Unknown"] = "Unknown"
    Source["UI"] = "UI"
    Source["Website"] = "Website"
    Source["System"] = "System"
    Source["Migration"] = "Migration"
})(Source || (Source = {}));
/** @typedef {number} */
export var MailingList;
(function (MailingList) {
    MailingList[MailingList["None"] = 0] = "None"
    MailingList[MailingList["TestGroup"] = 1] = "TestGroup"
    MailingList[MailingList["MonthlyNewsletter"] = 2] = "MonthlyNewsletter"
    MailingList[MailingList["BlogPostReleases"] = 4] = "BlogPostReleases"
    MailingList[MailingList["VideoReleases"] = 8] = "VideoReleases"
    MailingList[MailingList["ProductReleases"] = 16] = "ProductReleases"
    MailingList[MailingList["YearlyUpdates"] = 32] = "YearlyUpdates"
})(MailingList || (MailingList = {}));
export class Contact {
    /** @param {{id?:number,email?:string,firstName?:string,lastName?:string,source?:Source,mailingLists?:MailingList,token?:string,emailLower?:string,nameLower?:string,externalRef?:string,appUserId?:number,createdDate?:string,verifiedDate?:string,deletedDate?:string,unsubscribedDate?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    email;
    /** @type {string} */
    firstName;
    /** @type {?string} */
    lastName;
    /** @type {Source} */
    source;
    /** @type {MailingList} */
    mailingLists;
    /** @type {string} */
    token;
    /** @type {string} */
    emailLower;
    /** @type {string} */
    nameLower;
    /** @type {string} */
    externalRef;
    /** @type {?number} */
    appUserId;
    /** @type {string} */
    createdDate;
    /** @type {?string} */
    verifiedDate;
    /** @type {?string} */
    deletedDate;
    /** @type {?string} */
    unsubscribedDate;
}
export class ChoiceMessage {
    /** @param {{content?:string,tool_calls?:ToolCall[],role?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {string}
     * @description The contents of the message. */
    content;
    /**
     * @type {ToolCall[]}
     * @description The tool calls generated by the model, such as function calls. */
    tool_calls;
    /**
     * @type {string}
     * @description The role of the author of this message. */
    role;
}
export class Choice {
    /** @param {{finish_reason?:string,index?:number,message?:ChoiceMessage}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {string}
     * @description The reason the model stopped generating tokens. This will be stop if the model hit a natural stop point or a provided stop sequence, length if the maximum number of tokens specified in the request was reached, content_filter if content was omitted due to a flag from our content filters, tool_calls if the model called a tool */
    finish_reason;
    /**
     * @type {number}
     * @description The index of the choice in the list of choices. */
    index;
    /**
     * @type {ChoiceMessage}
     * @description A chat completion message generated by the model. */
    message;
}
export class OpenAiUsage {
    /** @param {{completion_tokens?:number,prompt_tokens?:number,total_tokens?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {number}
     * @description Number of tokens in the generated completion. */
    completion_tokens;
    /**
     * @type {number}
     * @description Number of tokens in the prompt. */
    prompt_tokens;
    /**
     * @type {number}
     * @description Total number of tokens used in the request (prompt + completion). */
    total_tokens;
}
export class OpenAiChatResponse {
    /** @param {{id?:string,choices?:Choice[],created?:number,model?:string,system_fingerprint?:string,object?:string,usage?:OpenAiUsage}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {string}
     * @description A unique identifier for the chat completion. */
    id;
    /**
     * @type {Choice[]}
     * @description A list of chat completion choices. Can be more than one if n is greater than 1. */
    choices;
    /**
     * @type {number}
     * @description The Unix timestamp (in seconds) of when the chat completion was created. */
    created;
    /**
     * @type {string}
     * @description The model used for the chat completion. */
    model;
    /**
     * @type {string}
     * @description This fingerprint represents the backend configuration that the model runs with. */
    system_fingerprint;
    /**
     * @type {string}
     * @description The object type, which is always chat.completion. */
    object;
    /**
     * @type {OpenAiUsage}
     * @description Usage statistics for the completion request. */
    usage;
}
export class Post {
    /** @param {{id?:number,postTypeId?:number,acceptedAnswerId?:number,parentId?:number,score?:number,viewCount?:number,title?:string,favoriteCount?:number,creationDate?:string,lastActivityDate?:string,lastEditDate?:string,lastEditorUserId?:number,ownerUserId?:number,tags?:string[],slug?:string,summary?:string,rankDate?:string,answerCount?:number,createdBy?:string,modifiedBy?:string,body?:string,modifiedReason?:string,lockedDate?:string,lockedReason?:string,refId?:string,refUrn?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    postTypeId;
    /** @type {?number} */
    acceptedAnswerId;
    /** @type {?number} */
    parentId;
    /** @type {number} */
    score;
    /** @type {?number} */
    viewCount;
    /** @type {string} */
    title;
    /** @type {?number} */
    favoriteCount;
    /** @type {string} */
    creationDate;
    /** @type {string} */
    lastActivityDate;
    /** @type {?string} */
    lastEditDate;
    /** @type {?number} */
    lastEditorUserId;
    /** @type {?number} */
    ownerUserId;
    /** @type {string[]} */
    tags;
    /** @type {string} */
    slug;
    /** @type {string} */
    summary;
    /** @type {?string} */
    rankDate;
    /** @type {?number} */
    answerCount;
    /** @type {?string} */
    createdBy;
    /** @type {?string} */
    modifiedBy;
    /** @type {?string} */
    body;
    /** @type {?string} */
    modifiedReason;
    /** @type {?string} */
    lockedDate;
    /** @type {?string} */
    lockedReason;
    /** @type {?string} */
    refId;
    /** @type {?string} */
    refUrn;
    /** @type {?{ [index: string]: string; }} */
    meta;
}
export class CreateAnswerTasks {
    /** @param {{post?:Post,modelUsers?:string[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {Post} */
    post;
    /** @type {string[]} */
    modelUsers;
}
export class CreateRankAnswerTask {
    /** @param {{answerId?:string,userId?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    answerId;
    /** @type {string} */
    userId;
}
export class Comment {
    /** @param {{body?:string,created?:number,createdBy?:string,upVotes?:number,reports?:number,aiRef?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    body;
    /** @type {number} */
    created;
    /** @type {string} */
    createdBy;
    /** @type {?number} */
    upVotes;
    /** @type {?number} */
    reports;
    /** @type {?string} */
    aiRef;
}
export class CreateAnswerCommentTask {
    /** @param {{aiRef?:string,model?:string,question?:Post,answer?:Post,userId?:string,userName?:string,comments?:Comment[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    aiRef;
    /** @type {string} */
    model;
    /** @type {Post} */
    question;
    /** @type {Post} */
    answer;
    /** @type {string} */
    userId;
    /** @type {string} */
    userName;
    /** @type {Comment[]} */
    comments;
}
/** @typedef {'Unknown'|'StackOverflow'|'Discourse'|'Reddit'|'GitHubDiscussions'} */
export var ImportSite;
(function (ImportSite) {
    ImportSite["Unknown"] = "Unknown"
    ImportSite["StackOverflow"] = "StackOverflow"
    ImportSite["Discourse"] = "Discourse"
    ImportSite["Reddit"] = "Reddit"
    ImportSite["GitHubDiscussions"] = "GitHubDiscussions"
})(ImportSite || (ImportSite = {}));
/** @typedef {'Unknown'|'Spam'|'Offensive'|'Duplicate'|'NotRelevant'|'LowQuality'|'Plagiarized'|'NeedsReview'} */
export var FlagType;
(function (FlagType) {
    FlagType["Unknown"] = "Unknown"
    FlagType["Spam"] = "Spam"
    FlagType["Offensive"] = "Offensive"
    FlagType["Duplicate"] = "Duplicate"
    FlagType["NotRelevant"] = "NotRelevant"
    FlagType["LowQuality"] = "LowQuality"
    FlagType["Plagiarized"] = "Plagiarized"
    FlagType["NeedsReview"] = "NeedsReview"
})(FlagType || (FlagType = {}));
export class RegenerateMeta {
    /** @param {{ifPostModified?:number,forPost?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?number} */
    ifPostModified;
    /** @type {?number} */
    forPost;
}
export class RenderHome {
    /** @param {{tab?:string,posts?:Post[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    tab;
    /** @type {Post[]} */
    posts;
}
export class QueryBase {
    /** @param {{skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?number} */
    skip;
    /** @type {?number} */
    take;
    /** @type {string} */
    orderBy;
    /** @type {string} */
    orderByDesc;
    /** @type {string} */
    include;
    /** @type {string} */
    fields;
    /** @type {{ [index: string]: string; }} */
    meta;
}
/** @typedef T {any} */
export class QueryDb extends QueryBase {
    /** @param {{skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
}
/** @typedef {'Invalid'|'AcceptAll'|'Unknown'|'Disposable'} */
export var InvalidEmailStatus;
(function (InvalidEmailStatus) {
    InvalidEmailStatus["Invalid"] = "Invalid"
    InvalidEmailStatus["AcceptAll"] = "AcceptAll"
    InvalidEmailStatus["Unknown"] = "Unknown"
    InvalidEmailStatus["Disposable"] = "Disposable"
})(InvalidEmailStatus || (InvalidEmailStatus = {}));
export class InvalidEmail {
    /** @param {{id?:number,email?:string,emailLower?:string,status?:InvalidEmailStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    email;
    /** @type {string} */
    emailLower;
    /** @type {InvalidEmailStatus} */
    status;
}
export class ArchiveMessage extends MailMessage {
    /** @param {{id?:number,email?:string,layout?:string,template?:string,renderer?:string,rendererArgs?:{ [index: string]: Object; },message?:EmailMessage,draft?:boolean,externalRef?:string,createdDate?:string,startedDate?:string,completedDate?:string,error?:ResponseStatus}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
}
export class MailRun {
    /** @param {{id?:number,mailingList?:MailingList,generator?:string,generatorArgs?:{ [index: string]: Object; },layout?:string,template?:string,externalRef?:string,createdDate?:string,generatedDate?:string,sentDate?:string,completedDate?:string,emailsCount?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {MailingList} */
    mailingList;
    /** @type {string} */
    generator;
    /** @type {{ [index: string]: Object; }} */
    generatorArgs;
    /** @type {string} */
    layout;
    /** @type {string} */
    template;
    /** @type {string} */
    externalRef;
    /** @type {string} */
    createdDate;
    /** @type {?string} */
    generatedDate;
    /** @type {?string} */
    sentDate;
    /** @type {?string} */
    completedDate;
    /** @type {number} */
    emailsCount;
}
export class ArchiveRun extends MailRun {
    /** @param {{id?:number,mailingList?:MailingList,generator?:string,generatorArgs?:{ [index: string]: Object; },layout?:string,template?:string,externalRef?:string,createdDate?:string,generatedDate?:string,sentDate?:string,completedDate?:string,emailsCount?:number}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
}
export class ArchiveMessageRun {
    /** @param {{id?:number,mailRunId?:number,contactId?:number,renderer?:string,rendererArgs?:{ [index: string]: Object; },externalRef?:string,message?:EmailMessage,createdDate?:string,startedDate?:string,completedDate?:string,error?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    mailRunId;
    /** @type {number} */
    contactId;
    /** @type {string} */
    renderer;
    /** @type {{ [index: string]: Object; }} */
    rendererArgs;
    /** @type {string} */
    externalRef;
    /** @type {EmailMessage} */
    message;
    /** @type {string} */
    createdDate;
    /** @type {?string} */
    startedDate;
    /** @type {?string} */
    completedDate;
    /** @type {?ResponseStatus} */
    error;
}
export class MailMessageRun {
    /** @param {{id?:number,mailRunId?:number,contactId?:number,contact?:Contact,renderer?:string,rendererArgs?:{ [index: string]: Object; },externalRef?:string,message?:EmailMessage,createdDate?:string,startedDate?:string,completedDate?:string,error?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    mailRunId;
    /** @type {number} */
    contactId;
    /** @type {Contact} */
    contact;
    /** @type {string} */
    renderer;
    /** @type {{ [index: string]: Object; }} */
    rendererArgs;
    /** @type {string} */
    externalRef;
    /** @type {EmailMessage} */
    message;
    /** @type {string} */
    createdDate;
    /** @type {?string} */
    startedDate;
    /** @type {?string} */
    completedDate;
    /** @type {?ResponseStatus} */
    error;
}
export class PageStats {
    /** @param {{label?:string,total?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    label;
    /** @type {number} */
    total;
}
export class PostJob {
    /** @param {{id?:number,postId?:number,model?:string,title?:string,createdBy?:string,createdDate?:string,startedDate?:string,worker?:string,workerIp?:string,completedDate?:string,error?:string,retryCount?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    postId;
    /** @type {string} */
    model;
    /** @type {string} */
    title;
    /** @type {string} */
    createdBy;
    /** @type {string} */
    createdDate;
    /** @type {?string} */
    startedDate;
    /** @type {?string} */
    worker;
    /** @type {?string} */
    workerIp;
    /** @type {?string} */
    completedDate;
    /** @type {?string} */
    error;
    /** @type {number} */
    retryCount;
}
export class ModelTotalStartUpVotes {
    /** @param {{id?:string,startingUpVotes?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    /** @type {number} */
    startingUpVotes;
}
export class LeaderBoardWinRate {
    /** @param {{id?:string,winRate?:number,numberOfQuestions?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    /** @type {number} */
    winRate;
    /** @type {number} */
    numberOfQuestions;
}
export class ModelTotalScore {
    /** @param {{id?:string,totalScore?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    /** @type {number} */
    totalScore;
}
export class ModelWinRate {
    /** @param {{id?:string,winRate?:number,numberOfQuestions?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    /** @type {number} */
    winRate;
    /** @type {number} */
    numberOfQuestions;
}
export class StatTotals {
    /** @param {{id?:string,postId?:number,createdBy?:string,favoriteCount?:number,viewCount?:number,upVotes?:number,downVotes?:number,startingUpVotes?:number,lastUpdated?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    /** @type {number} */
    postId;
    /** @type {?string} */
    createdBy;
    /** @type {number} */
    favoriteCount;
    /** @type {number} */
    viewCount;
    /** @type {number} */
    upVotes;
    /** @type {number} */
    downVotes;
    /** @type {number} */
    startingUpVotes;
    /** @type {?string} */
    lastUpdated;
}
/** @typedef {'Unknown'|'NewComment'|'NewAnswer'|'QuestionMention'|'AnswerMention'|'CommentMention'} */
export var NotificationType;
(function (NotificationType) {
    NotificationType["Unknown"] = "Unknown"
    NotificationType["NewComment"] = "NewComment"
    NotificationType["NewAnswer"] = "NewAnswer"
    NotificationType["QuestionMention"] = "QuestionMention"
    NotificationType["AnswerMention"] = "AnswerMention"
    NotificationType["CommentMention"] = "CommentMention"
})(NotificationType || (NotificationType = {}));
export class Notification {
    /** @param {{id?:number,userName?:string,type?:NotificationType,postId?:number,refId?:string,summary?:string,createdDate?:string,read?:boolean,href?:string,title?:string,refUserName?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    userName;
    /** @type {NotificationType} */
    type;
    /** @type {number} */
    postId;
    /** @type {string} */
    refId;
    /** @type {string} */
    summary;
    /** @type {string} */
    createdDate;
    /** @type {boolean} */
    read;
    /** @type {?string} */
    href;
    /** @type {?string} */
    title;
    /** @type {?string} */
    refUserName;
}
/** @typedef {'Unknown'|'NewAnswer'|'AnswerUpVote'|'AnswerDownVote'|'NewQuestion'|'QuestionUpVote'|'QuestionDownVote'} */
export var AchievementType;
(function (AchievementType) {
    AchievementType["Unknown"] = "Unknown"
    AchievementType["NewAnswer"] = "NewAnswer"
    AchievementType["AnswerUpVote"] = "AnswerUpVote"
    AchievementType["AnswerDownVote"] = "AnswerDownVote"
    AchievementType["NewQuestion"] = "NewQuestion"
    AchievementType["QuestionUpVote"] = "QuestionUpVote"
    AchievementType["QuestionDownVote"] = "QuestionDownVote"
})(AchievementType || (AchievementType = {}));
export class Achievement {
    /** @param {{id?:number,userName?:string,type?:AchievementType,postId?:number,refId?:string,refUserName?:string,score?:number,read?:boolean,href?:string,title?:string,createdDate?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    userName;
    /** @type {AchievementType} */
    type;
    /** @type {number} */
    postId;
    /** @type {string} */
    refId;
    /** @type {?string} */
    refUserName;
    /** @type {number} */
    score;
    /** @type {boolean} */
    read;
    /** @type {?string} */
    href;
    /** @type {?string} */
    title;
    /** @type {string} */
    createdDate;
}
export class ToolCall {
    /** @param {{id?:string,type?:string,function?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {string}
     * @description The ID of the tool call. */
    id;
    /**
     * @type {string}
     * @description The type of the tool. Currently, only `function` is supported. */
    type;
    /**
     * @type {string}
     * @description The function that the model called. */
    function;
}
export class FindContactResponse {
    /** @param {{result?:Contact,responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {Contact} */
    result;
    /** @type {ResponseStatus} */
    responseStatus;
}
export class ViewMailRunInfoResponse {
    /** @param {{messagesSent?:number,totalMessages?:number,timeTaken?:string,responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    messagesSent;
    /** @type {number} */
    totalMessages;
    /** @type {string} */
    timeTaken;
    /** @type {ResponseStatus} */
    responseStatus;
}
export class ViewAppDataResponse {
    /** @param {{websiteBaseUrl?:string,baseUrl?:string,vars?:{ [index: string]: { [index:string]: string; }; },bannedUserIds?:number[],responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    websiteBaseUrl;
    /** @type {string} */
    baseUrl;
    /** @type {{ [index: string]: { [index:string]: string; }; }} */
    vars;
    /** @type {number[]} */
    bannedUserIds;
    /** @type {ResponseStatus} */
    responseStatus;
}
export class ViewAppStatsResponse {
    /** @param {{totals?:{ [index: string]: number; },before30DayTotals?:{ [index: string]: number; },last30DayTotals?:{ [index: string]: number; },archivedTotals?:{ [index: string]: number; },responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {{ [index: string]: number; }} */
    totals;
    /** @type {{ [index: string]: number; }} */
    before30DayTotals;
    /** @type {{ [index: string]: number; }} */
    last30DayTotals;
    /** @type {{ [index: string]: number; }} */
    archivedTotals;
    /** @type {ResponseStatus} */
    responseStatus;
}
export class ArchiveMailResponse {
    /** @param {{archivedMessageIds?:number[],archivedMailRunIds?:number[],responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number[]} */
    archivedMessageIds;
    /** @type {number[]} */
    archivedMailRunIds;
    /** @type {ResponseStatus} */
    responseStatus;
}
export class HelloResponse {
    /** @param {{result?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    result;
}
export class AdminDataResponse {
    /** @param {{pageStats?:PageStats[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {PageStats[]} */
    pageStats;
}
export class StringResponse {
    /** @param {{result?:string,meta?:{ [index: string]: string; },responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    result;
    /** @type {{ [index: string]: string; }} */
    meta;
    /** @type {ResponseStatus} */
    responseStatus;
}
export class Meta {
    /** @param {{id?:number,modelVotes?:{ [index: string]: number; },modelReasons?:{ [index: string]: string; },gradedBy?:{ [index: string]: string; },comments?:{ [index: string]: Comment[]; },statTotals?:StatTotals[],modifiedDate?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {{ [index: string]: number; }} */
    modelVotes;
    /** @type {{ [index: string]: string; }} */
    modelReasons;
    /** @type {{ [index: string]: string; }} */
    gradedBy;
    /** @type {{ [index: string]: Comment[]; }} */
    comments;
    /** @type {StatTotals[]} */
    statTotals;
    /** @type {string} */
    modifiedDate;
}
export class QuestionAndAnswers {
    /** @param {{id?:number,post?:Post,meta?:Meta,answers?:Post[],viewCount?:number,questionScore?:number,questionComments?:Comment[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {Post} */
    post;
    /** @type {?Meta} */
    meta;
    /** @type {Post[]} */
    answers;
    /** @type {number} */
    viewCount;
    /** @type {number} */
    questionScore;
    /** @type {Comment[]} */
    questionComments;
}
export class SearchPostsResponse {
    /** @param {{total?:number,results?:Post[],responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    total;
    /** @type {Post[]} */
    results;
    /** @type {?ResponseStatus} */
    responseStatus;
}
export class ViewModelQueuesResponse {
    /** @param {{jobs?:PostJob[],responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {PostJob[]} */
    jobs;
    /** @type {ResponseStatus} */
    responseStatus;
}
export class GetNextJobsResponse {
    /** @param {{results?:PostJob[],responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {PostJob[]} */
    results;
    /** @type {ResponseStatus} */
    responseStatus;
}
export class StringsResponse {
    /** @param {{results?:string[],meta?:{ [index: string]: string; },responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string[]} */
    results;
    /** @type {{ [index: string]: string; }} */
    meta;
    /** @type {ResponseStatus} */
    responseStatus;
}
export class CalculateLeaderboardResponse {
    /** @param {{mostLikedModelsByLlm?:ModelTotalStartUpVotes[],answererWinRate?:LeaderBoardWinRate[],modelTotalScore?:ModelTotalScore[],modelWinRate?:ModelWinRate[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {ModelTotalStartUpVotes[]} */
    mostLikedModelsByLlm;
    /** @type {LeaderBoardWinRate[]} */
    answererWinRate;
    /** @type {ModelTotalScore[]} */
    modelTotalScore;
    /** @type {ModelWinRate[]} */
    modelWinRate;
}
export class GetAllAnswerModelsResponse {
    /** @param {{results?:string[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string[]} */
    results;
}
export class FindSimilarQuestionsResponse {
    /** @param {{results?:Post[],responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {Post[]} */
    results;
    /** @type {?ResponseStatus} */
    responseStatus;
}
export class AskQuestionResponse {
    /** @param {{id?:number,slug?:string,redirectTo?:string,responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    slug;
    /** @type {?string} */
    redirectTo;
    /** @type {?ResponseStatus} */
    responseStatus;
}
export class EmptyResponse {
    /** @param {{responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {ResponseStatus} */
    responseStatus;
}
export class AnswerQuestionResponse {
    /** @param {{responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?ResponseStatus} */
    responseStatus;
}
export class UpdateQuestionResponse {
    /** @param {{result?:Post,responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {Post} */
    result;
    /** @type {?ResponseStatus} */
    responseStatus;
}
export class UpdateAnswerResponse {
    /** @param {{responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?ResponseStatus} */
    responseStatus;
}
export class GetQuestionResponse {
    /** @param {{result?:Post,responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {Post} */
    result;
    /** @type {?ResponseStatus} */
    responseStatus;
}
export class GetAnswerResponse {
    /** @param {{result?:Post}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {Post} */
    result;
}
export class CommentsResponse {
    /** @param {{aiRef?:string,lastUpdated?:number,comments?:Comment[],responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    aiRef;
    /** @type {number} */
    lastUpdated;
    /** @type {Comment[]} */
    comments;
    /** @type {ResponseStatus} */
    responseStatus;
}
export class GetUserReputationsResponse {
    /** @param {{results?:{ [index: string]: string; },responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {{ [index: string]: string; }} */
    results;
    /** @type {?ResponseStatus} */
    responseStatus;
}
export class AskQuestion {
    /** @param {{title?:string,body?:string,tags?:string[],refId?:string,refUrn?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    title;
    /** @type {string} */
    body;
    /** @type {string[]} */
    tags;
    /** @type {?string} */
    refId;
    /** @type {?string} */
    refUrn;
    getTypeName() { return 'AskQuestion' }
    getMethod() { return 'POST' }
    createResponse() { return new AskQuestionResponse() }
}
export class ImportQuestionResponse {
    /** @param {{result?:AskQuestion,responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {AskQuestion} */
    result;
    /** @type {?ResponseStatus} */
    responseStatus;
}
export class GetLastUpdatedResponse {
    /** @param {{result?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?number} */
    result;
}
export class UpdateUserProfileResponse {
    /** @param {{responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {ResponseStatus} */
    responseStatus;
}
export class UserPostDataResponse {
    /** @param {{watching?:boolean,questionsAsked?:number,upVoteIds?:string[],downVoteIds?:string[],responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {boolean} */
    watching;
    /** @type {number} */
    questionsAsked;
    /** @type {string[]} */
    upVoteIds;
    /** @type {string[]} */
    downVoteIds;
    /** @type {?ResponseStatus} */
    responseStatus;
}
export class GetLatestNotificationsResponse {
    /** @param {{hasUnread?:boolean,results?:Notification[],responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {boolean} */
    hasUnread;
    /** @type {Notification[]} */
    results;
    /** @type {?ResponseStatus} */
    responseStatus;
}
export class GetLatestAchievementsResponse {
    /** @param {{hasUnread?:boolean,results?:Achievement[],responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {boolean} */
    hasUnread;
    /** @type {Achievement[]} */
    results;
    /** @type {?ResponseStatus} */
    responseStatus;
}
export class BoolResponse {
    /** @param {{result?:boolean,meta?:{ [index: string]: string; },responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {boolean} */
    result;
    /** @type {{ [index: string]: string; }} */
    meta;
    /** @type {ResponseStatus} */
    responseStatus;
}
export class GetWatchedTagsResponse {
    /** @param {{results?:string[],responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string[]} */
    results;
    /** @type {?ResponseStatus} */
    responseStatus;
}
/** @typedef T {any} */
export class QueryResponse {
    /** @param {{offset?:number,total?:number,results?:T[],meta?:{ [index: string]: string; },responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    offset;
    /** @type {number} */
    total;
    /** @type {T[]} */
    results;
    /** @type {{ [index: string]: string; }} */
    meta;
    /** @type {ResponseStatus} */
    responseStatus;
}
export class AuthenticateResponse {
    /** @param {{userId?:string,sessionId?:string,userName?:string,displayName?:string,referrerUrl?:string,bearerToken?:string,refreshToken?:string,refreshTokenExpiry?:string,profileUrl?:string,roles?:string[],permissions?:string[],responseStatus?:ResponseStatus,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    userId;
    /** @type {string} */
    sessionId;
    /** @type {string} */
    userName;
    /** @type {string} */
    displayName;
    /** @type {string} */
    referrerUrl;
    /** @type {string} */
    bearerToken;
    /** @type {string} */
    refreshToken;
    /** @type {?string} */
    refreshTokenExpiry;
    /** @type {string} */
    profileUrl;
    /** @type {string[]} */
    roles;
    /** @type {string[]} */
    permissions;
    /** @type {ResponseStatus} */
    responseStatus;
    /** @type {{ [index: string]: string; }} */
    meta;
}
export class CreatorKitTasks {
    /** @param {{sendMessages?:SendMailMessages}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?SendMailMessages} */
    sendMessages;
    getTypeName() { return 'CreatorKitTasks' }
    getMethod() { return 'POST' }
    createResponse () { };
}
export class PreviewEmail {
    /** @param {{request?:string,renderer?:string,requestArgs?:{ [index: string]: Object; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    request;
    /** @type {?string} */
    renderer;
    /** @type {{ [index: string]: Object; }} */
    requestArgs;
    getTypeName() { return 'PreviewEmail' }
    getMethod() { return 'POST' }
    createResponse() { return '' }
}
export class UpdateMailMessageDraft {
    /** @param {{id?:number,email?:string,renderer?:string,layout?:string,template?:string,subject?:string,body?:string,send?:boolean}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    email;
    /** @type {string} */
    renderer;
    /** @type {?string} */
    layout;
    /** @type {?string} */
    template;
    /** @type {string} */
    subject;
    /** @type {?string} */
    body;
    /** @type {?boolean} */
    send;
    getTypeName() { return 'UpdateMailMessageDraft' }
    getMethod() { return 'POST' }
    createResponse() { return new MailMessage() }
}
export class SimpleTextEmail extends CreateEmailBase {
    /** @param {{subject?:string,body?:string,draft?:boolean,email?:string,firstName?:string,lastName?:string}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {string} */
    subject;
    /** @type {string} */
    body;
    /** @type {?boolean} */
    draft;
    getTypeName() { return 'SimpleTextEmail' }
    getMethod() { return 'POST' }
    createResponse() { return new MailMessage() }
}
export class CustomHtmlEmail extends CreateEmailBase {
    /** @param {{layout?:string,template?:string,subject?:string,body?:string,draft?:boolean,email?:string,firstName?:string,lastName?:string}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {string} */
    layout;
    /** @type {string} */
    template;
    /** @type {string} */
    subject;
    /** @type {?string} */
    body;
    /** @type {?boolean} */
    draft;
    getTypeName() { return 'CustomHtmlEmail' }
    getMethod() { return 'POST' }
    createResponse() { return new MailMessage() }
}
export class SubscribeToMailingList {
    /** @param {{email?:string,firstName?:string,lastName?:string,source?:Source,mailingLists?:string[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    email;
    /** @type {string} */
    firstName;
    /** @type {string} */
    lastName;
    /** @type {Source} */
    source;
    /** @type {?string[]} */
    mailingLists;
    getTypeName() { return 'SubscribeToMailingList' }
    getMethod() { return 'POST' }
    createResponse() { }
}
export class CreateContact {
    /** @param {{email?:string,firstName?:string,lastName?:string,source?:Source,mailingLists?:string[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    email;
    /** @type {string} */
    firstName;
    /** @type {string} */
    lastName;
    /** @type {Source} */
    source;
    /** @type {?string[]} */
    mailingLists;
    getTypeName() { return 'CreateContact' }
    getMethod() { return 'POST' }
    createResponse() { return new Contact() }
}
export class AdminCreateContact {
    /** @param {{email?:string,firstName?:string,lastName?:string,source?:Source,mailingLists?:string[],verifiedDate?:string,appUserId?:number,createdDate?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    email;
    /** @type {string} */
    firstName;
    /** @type {string} */
    lastName;
    /** @type {Source} */
    source;
    /** @type {string[]} */
    mailingLists;
    /** @type {?string} */
    verifiedDate;
    /** @type {?number} */
    appUserId;
    /** @type {?string} */
    createdDate;
    getTypeName() { return 'AdminCreateContact' }
    getMethod() { return 'POST' }
    createResponse() { return new Contact() }
}
export class UpdateContactMailingLists {
    /** @param {{ref?:string,mailingLists?:string[],unsubscribeAll?:boolean}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    ref;
    /** @type {string[]} */
    mailingLists;
    /** @type {?boolean} */
    unsubscribeAll;
    getTypeName() { return 'UpdateContactMailingLists' }
    getMethod() { return 'POST' }
    createResponse() { }
}
export class FindContact {
    /** @param {{email?:string,ref?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    email;
    /** @type {?string} */
    ref;
    getTypeName() { return 'FindContact' }
    getMethod() { return 'GET' }
    createResponse() { return new FindContactResponse() }
}
export class SendMailMessage {
    /** @param {{id?:number,force?:boolean}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?boolean} */
    force;
    getTypeName() { return 'SendMailMessage' }
    getMethod() { return 'GET' }
    createResponse() { return new MailMessage() }
}
export class SendMailMessageRun {
    /** @param {{id?:number,force?:boolean}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?boolean} */
    force;
    getTypeName() { return 'SendMailMessageRun' }
    getMethod() { return 'GET' }
    createResponse() { return new MailMessage() }
}
export class VerifyEmailAddress {
    /** @param {{externalRef?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    externalRef;
    getTypeName() { return 'VerifyEmailAddress' }
    getMethod() { return 'POST' }
    createResponse() { }
}
export class SendMailRun {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'SendMailRun' }
    getMethod() { return 'POST' }
    createResponse() { }
}
export class ViewMailRunInfo {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'ViewMailRunInfo' }
    getMethod() { return 'GET' }
    createResponse() { return new ViewMailRunInfoResponse() }
}
export class ViewAppData {
    constructor(init) { Object.assign(this, init) }
    getTypeName() { return 'ViewAppData' }
    getMethod() { return 'GET' }
    createResponse() { return new ViewAppDataResponse() }
}
export class ViewAppStats {
    constructor(init) { Object.assign(this, init) }
    getTypeName() { return 'ViewAppStats' }
    getMethod() { return 'GET' }
    createResponse() { return new ViewAppStatsResponse() }
}
export class ArchiveMail {
    /** @param {{messages?:boolean,mailRuns?:boolean,olderThanDays?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?boolean} */
    messages;
    /** @type {?boolean} */
    mailRuns;
    /** @type {number} */
    olderThanDays;
    getTypeName() { return 'ArchiveMail' }
    getMethod() { return 'POST' }
    createResponse() { return new ArchiveMailResponse() }
}
export class Hello {
    /** @param {{name?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    name;
    getTypeName() { return 'Hello' }
    getMethod() { return 'GET' }
    createResponse() { return new HelloResponse() }
}
export class AdminData {
    constructor(init) { Object.assign(this, init) }
    getTypeName() { return 'AdminData' }
    getMethod() { return 'GET' }
    createResponse() { return new AdminDataResponse() }
}
export class GetRequestInfo {
    constructor(init) { Object.assign(this, init) }
    getTypeName() { return 'GetRequestInfo' }
    getMethod() { return 'GET' }
    createResponse() { return '' }
}
export class Sync {
    /** @param {{tasks?:string[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string[]} */
    tasks;
    getTypeName() { return 'Sync' }
    getMethod() { return 'GET' }
    createResponse() { return new StringResponse() }
}
export class GenerateMeta {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'GenerateMeta' }
    getMethod() { return 'GET' }
    createResponse() { return new QuestionAndAnswers() }
}
export class AdminResetCommonPassword {
    constructor(init) { Object.assign(this, init) }
    getTypeName() { return 'AdminResetCommonPassword' }
    getMethod() { return 'POST' }
    createResponse () { };
}
export class ResaveQuestionFromFile {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'ResaveQuestionFromFile' }
    getMethod() { return 'POST' }
    createResponse() { return new Post() }
}
export class RankAnswer {
    /** @param {{id?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    getTypeName() { return 'RankAnswer' }
    getMethod() { return 'POST' }
    createResponse() { return new Post() }
}
export class CreateAnswerCallback extends OpenAiChatResponse {
    /** @param {{postId?:number,userId?:string,id?:string,choices?:Choice[],created?:number,model?:string,system_fingerprint?:string,object?:string,usage?:OpenAiUsage}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {number} */
    postId;
    /** @type {string} */
    userId;
    getTypeName() { return 'CreateAnswerCallback' }
    getMethod() { return 'POST' }
    createResponse() { }
}
export class RankAnswerCallback extends OpenAiChatResponse {
    /** @param {{postId?:number,userId?:string,grader?:string,id?:string,choices?:Choice[],created?:number,model?:string,system_fingerprint?:string,object?:string,usage?:OpenAiUsage}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {number} */
    postId;
    /** @type {string} */
    userId;
    /** @type {string} */
    grader;
    getTypeName() { return 'RankAnswerCallback' }
    getMethod() { return 'POST' }
    createResponse() { }
}
export class AnswerCommentCallback extends OpenAiChatResponse {
    /** @param {{answerId?:string,userId?:string,aiRef?:string,id?:string,choices?:Choice[],created?:number,model?:string,system_fingerprint?:string,object?:string,usage?:OpenAiUsage}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {string} */
    answerId;
    /** @type {string} */
    userId;
    /** @type {string} */
    aiRef;
    getTypeName() { return 'AnswerCommentCallback' }
    getMethod() { return 'POST' }
    createResponse() { }
}
export class SearchPosts {
    /** @param {{q?:string,view?:string,skip?:number,take?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    q;
    /** @type {?string} */
    view;
    /** @type {?number} */
    skip;
    /** @type {?number} */
    take;
    getTypeName() { return 'SearchPosts' }
    getMethod() { return 'GET' }
    createResponse() { return new SearchPostsResponse() }
}
export class AiServerTasks {
    /** @param {{createAnswerTasks?:CreateAnswerTasks,createRankAnswerTask?:CreateRankAnswerTask,createAnswerCommentTask?:CreateAnswerCommentTask}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?CreateAnswerTasks} */
    createAnswerTasks;
    /** @type {?CreateRankAnswerTask} */
    createRankAnswerTask;
    /** @type {?CreateAnswerCommentTask} */
    createAnswerCommentTask;
    getTypeName() { return 'AiServerTasks' }
    getMethod() { return 'POST' }
    createResponse () { };
}
export class DeleteCdnFilesMq {
    /** @param {{files?:string[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string[]} */
    files;
    getTypeName() { return 'DeleteCdnFilesMq' }
    getMethod() { return 'POST' }
    createResponse () { };
}
export class DeleteCdnFile {
    /** @param {{file?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    file;
    getTypeName() { return 'DeleteCdnFile' }
    getMethod() { return 'POST' }
    createResponse() { }
}
export class GetCdnFile {
    /** @param {{file?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    file;
    getTypeName() { return 'GetCdnFile' }
    getMethod() { return 'POST' }
    createResponse () { };
}
export class SendNewAnswerEmail {
    /** @param {{userName?:string,answerId?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    userName;
    /** @type {string} */
    answerId;
    getTypeName() { return 'SendNewAnswerEmail' }
    getMethod() { return 'GET' }
    createResponse() { return new StringResponse() }
}
export class ViewModelQueues {
    /** @param {{models?:string[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string[]} */
    models;
    getTypeName() { return 'ViewModelQueues' }
    getMethod() { return 'GET' }
    createResponse() { return new ViewModelQueuesResponse() }
}
export class GetNextJobs {
    /** @param {{models?:string[],worker?:string,take?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string[]} */
    models;
    /** @type {?string} */
    worker;
    /** @type {?number} */
    take;
    getTypeName() { return 'GetNextJobs' }
    getMethod() { return 'GET' }
    createResponse() { return new GetNextJobsResponse() }
}
export class FailJob {
    /** @param {{id?:number,error?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    error;
    getTypeName() { return 'FailJob' }
    getMethod() { return 'POST' }
    createResponse() { }
}
export class RestoreModelQueues {
    /** @param {{restoreFailedJobs?:boolean}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?boolean} */
    restoreFailedJobs;
    getTypeName() { return 'RestoreModelQueues' }
    getMethod() { return 'GET' }
    createResponse() { return new StringsResponse() }
}
export class CalculateLeaderBoard {
    /** @param {{modelsToExclude?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    modelsToExclude;
    getTypeName() { return 'CalculateLeaderBoard' }
    getMethod() { return 'GET' }
    createResponse() { return new CalculateLeaderboardResponse() }
}
export class GetLeaderboardStatsByTag {
    /** @param {{tag?:string,modelsToExclude?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    tag;
    /** @type {?string} */
    modelsToExclude;
    getTypeName() { return 'GetLeaderboardStatsByTag' }
    getMethod() { return 'POST' }
    createResponse () { };
}
export class GetAllAnswerModels {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'GetAllAnswerModels' }
    getMethod() { return 'GET' }
    createResponse() { return new GetAllAnswerModelsResponse() }
}
export class FindSimilarQuestions {
    /** @param {{text?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    text;
    getTypeName() { return 'FindSimilarQuestions' }
    getMethod() { return 'GET' }
    createResponse() { return new FindSimilarQuestionsResponse() }
}
export class DeleteQuestion {
    /** @param {{id?:number,returnUrl?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    returnUrl;
    getTypeName() { return 'DeleteQuestion' }
    getMethod() { return 'GET' }
    createResponse() { return new EmptyResponse() }
}
export class AnswerQuestion {
    /** @param {{postId?:number,body?:string,refId?:string,refUrn?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    postId;
    /** @type {string} */
    body;
    /** @type {?string} */
    refId;
    /** @type {?string} */
    refUrn;
    getTypeName() { return 'AnswerQuestion' }
    getMethod() { return 'POST' }
    createResponse() { return new AnswerQuestionResponse() }
}
export class UpdateQuestion {
    /** @param {{id?:number,title?:string,body?:string,tags?:string[],editReason?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    title;
    /** @type {string} */
    body;
    /** @type {string[]} */
    tags;
    /** @type {string} */
    editReason;
    getTypeName() { return 'UpdateQuestion' }
    getMethod() { return 'POST' }
    createResponse() { return new UpdateQuestionResponse() }
}
export class UpdateAnswer {
    /** @param {{id?:string,body?:string,editReason?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    /** @type {string} */
    body;
    /** @type {string} */
    editReason;
    getTypeName() { return 'UpdateAnswer' }
    getMethod() { return 'POST' }
    createResponse() { return new UpdateAnswerResponse() }
}
export class GetQuestion {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'GetQuestion' }
    getMethod() { return 'GET' }
    createResponse() { return new GetQuestionResponse() }
}
export class GetQuestionFile {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'GetQuestionFile' }
    getMethod() { return 'GET' }
    createResponse() { return '' }
}
export class GetQuestionBody {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'GetQuestionBody' }
    getMethod() { return 'GET' }
    createResponse() { return '' }
}
export class GetAnswerFile {
    /** @param {{id?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    getTypeName() { return 'GetAnswerFile' }
    getMethod() { return 'GET' }
    createResponse() { return '' }
}
export class GetAnswer {
    /** @param {{id?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    getTypeName() { return 'GetAnswer' }
    getMethod() { return 'GET' }
    createResponse() { return new GetAnswerResponse() }
}
export class GetAnswerBody {
    /** @param {{id?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    getTypeName() { return 'GetAnswerBody' }
    getMethod() { return 'GET' }
    createResponse() { return '' }
}
export class CreateComment {
    /** @param {{id?:string,body?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    /** @type {string} */
    body;
    getTypeName() { return 'CreateComment' }
    getMethod() { return 'POST' }
    createResponse() { return new CommentsResponse() }
}
export class DeleteComment {
    /** @param {{id?:string,createdBy?:string,created?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    /** @type {string} */
    createdBy;
    /** @type {number} */
    created;
    getTypeName() { return 'DeleteComment' }
    getMethod() { return 'POST' }
    createResponse() { return new CommentsResponse() }
}
export class GetMeta {
    /** @param {{id?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    getTypeName() { return 'GetMeta' }
    getMethod() { return 'GET' }
    createResponse() { return new Meta() }
}
export class GetUserReputations {
    /** @param {{userNames?:string[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string[]} */
    userNames;
    getTypeName() { return 'GetUserReputations' }
    getMethod() { return 'GET' }
    createResponse() { return new GetUserReputationsResponse() }
}
export class ImportQuestion {
    /** @param {{url?:string,site?:ImportSite,tags?:string[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    url;
    /** @type {ImportSite} */
    site;
    /** @type {?string[]} */
    tags;
    getTypeName() { return 'ImportQuestion' }
    getMethod() { return 'GET' }
    createResponse() { return new ImportQuestionResponse() }
}
export class GetLastUpdated {
    /** @param {{id?:string,postId?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    id;
    /** @type {?number} */
    postId;
    getTypeName() { return 'GetLastUpdated' }
    getMethod() { return 'GET' }
    createResponse() { return new GetLastUpdatedResponse() }
}
export class WaitForUpdate {
    /** @param {{id?:string,updatedAfter?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    /** @type {?number} */
    updatedAfter;
    getTypeName() { return 'WaitForUpdate' }
    getMethod() { return 'GET' }
    createResponse() { return new GetLastUpdatedResponse() }
}
export class UpdateUserProfile {
    constructor(init) { Object.assign(this, init) }
    getTypeName() { return 'UpdateUserProfile' }
    getMethod() { return 'POST' }
    createResponse() { return new UpdateUserProfileResponse() }
}
export class GetProfileImage {
    /** @param {{path?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    path;
    getTypeName() { return 'GetProfileImage' }
    getMethod() { return 'GET' }
    createResponse() { return new Blob() }
}
export class GetUserAvatar {
    /** @param {{userName?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    userName;
    getTypeName() { return 'GetUserAvatar' }
    getMethod() { return 'GET' }
    createResponse() { return new Blob() }
}
export class UserPostData {
    /** @param {{postId?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    postId;
    getTypeName() { return 'UserPostData' }
    getMethod() { return 'GET' }
    createResponse() { return new UserPostDataResponse() }
}
export class PostVote {
    /** @param {{refId?:string,up?:boolean,down?:boolean}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    refId;
    /** @type {?boolean} */
    up;
    /** @type {?boolean} */
    down;
    getTypeName() { return 'PostVote' }
    getMethod() { return 'POST' }
    createResponse() { }
}
export class CommentVote {
    /** @param {{refId?:string,up?:boolean,down?:boolean}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    refId;
    /** @type {?boolean} */
    up;
    /** @type {?boolean} */
    down;
    getTypeName() { return 'CommentVote' }
    getMethod() { return 'POST' }
    createResponse() { }
}
export class CreateAvatar {
    /** @param {{userName?:string,textColor?:string,bgColor?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    userName;
    /** @type {?string} */
    textColor;
    /** @type {?string} */
    bgColor;
    getTypeName() { return 'CreateAvatar' }
    getMethod() { return 'GET' }
    createResponse() { return '' }
}
export class GetLatestNotifications {
    constructor(init) { Object.assign(this, init) }
    getTypeName() { return 'GetLatestNotifications' }
    getMethod() { return 'GET' }
    createResponse() { return new GetLatestNotificationsResponse() }
}
export class GetLatestAchievements {
    constructor(init) { Object.assign(this, init) }
    getTypeName() { return 'GetLatestAchievements' }
    getMethod() { return 'GET' }
    createResponse() { return new GetLatestAchievementsResponse() }
}
export class MarkAsRead {
    /** @param {{notificationIds?:number[],allNotifications?:boolean,achievementIds?:number[],allAchievements?:boolean}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?number[]} */
    notificationIds;
    /** @type {?boolean} */
    allNotifications;
    /** @type {?number[]} */
    achievementIds;
    /** @type {?boolean} */
    allAchievements;
    getTypeName() { return 'MarkAsRead' }
    getMethod() { return 'POST' }
    createResponse() { return new EmptyResponse() }
}
export class ShareContent {
    /** @param {{refId?:string,userId?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    refId;
    /** @type {?number} */
    userId;
    getTypeName() { return 'ShareContent' }
    getMethod() { return 'GET' }
    createResponse() { return '' }
}
export class FlagContent {
    /** @param {{refId?:string,type?:FlagType,reason?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    refId;
    /** @type {FlagType} */
    type;
    /** @type {?string} */
    reason;
    getTypeName() { return 'FlagContent' }
    getMethod() { return 'POST' }
    createResponse() { return new EmptyResponse() }
}
export class WatchContent {
    /** @param {{postId?:number,tag?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?number} */
    postId;
    /** @type {?string} */
    tag;
    getTypeName() { return 'WatchContent' }
    getMethod() { return 'POST' }
    createResponse() { return new EmptyResponse() }
}
export class UnwatchContent {
    /** @param {{postId?:number,tag?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?number} */
    postId;
    /** @type {?string} */
    tag;
    getTypeName() { return 'UnwatchContent' }
    getMethod() { return 'POST' }
    createResponse() { return new EmptyResponse() }
}
export class WatchStatus {
    /** @param {{postId?:number,tag?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?number} */
    postId;
    /** @type {?string} */
    tag;
    getTypeName() { return 'WatchStatus' }
    getMethod() { return 'GET' }
    createResponse() { return new BoolResponse() }
}
export class WatchTags {
    /** @param {{subscribe?:string[],unsubscribe?:string[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string[]} */
    subscribe;
    /** @type {?string[]} */
    unsubscribe;
    getTypeName() { return 'WatchTags' }
    getMethod() { return 'POST' }
    createResponse() { return new EmptyResponse() }
}
export class GetWatchedTags {
    constructor(init) { Object.assign(this, init) }
    getTypeName() { return 'GetWatchedTags' }
    getMethod() { return 'GET' }
    createResponse() { return new GetWatchedTagsResponse() }
}
export class RenderComponent {
    /** @param {{regenerateMeta?:RegenerateMeta,question?:QuestionAndAnswers,home?:RenderHome}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?RegenerateMeta} */
    regenerateMeta;
    /** @type {?QuestionAndAnswers} */
    question;
    /** @type {?RenderHome} */
    home;
    getTypeName() { return 'RenderComponent' }
    getMethod() { return 'POST' }
    createResponse() { }
}
export class PreviewMarkdown {
    /** @param {{markdown?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    markdown;
    getTypeName() { return 'PreviewMarkdown' }
    getMethod() { return 'POST' }
    createResponse() { return '' }
}
export class QueryPosts extends QueryDb {
    /** @param {{skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    getTypeName() { return 'QueryPosts' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryContacts extends QueryDb {
    /** @param {{search?:string,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?string} */
    search;
    getTypeName() { return 'QueryContacts' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryInvalidEmails extends QueryDb {
    /** @param {{skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    getTypeName() { return 'QueryInvalidEmails' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryMailMessages extends QueryDb {
    /** @param {{skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    getTypeName() { return 'QueryMailMessages' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryMailRuns extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryMailRuns' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryMailRunMessages extends QueryDb {
    /** @param {{skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    getTypeName() { return 'QueryMailRunMessages' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryArchiveMessages extends QueryDb {
    /** @param {{skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    getTypeName() { return 'QueryArchiveMessages' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryArchiveRuns extends QueryDb {
    /** @param {{skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    getTypeName() { return 'QueryArchiveRuns' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryArchiveMessageRuns extends QueryDb {
    /** @param {{skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    getTypeName() { return 'QueryArchiveMessageRuns' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class UpdateContact {
    /** @param {{id?:number,email?:string,firstName?:string,lastName?:string,source?:Source,mailingLists?:string[],externalRef?:string,appUserId?:number,createdDate?:string,verifiedDate?:string,deletedDate?:string,unsubscribedDate?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    email;
    /** @type {?string} */
    firstName;
    /** @type {?string} */
    lastName;
    /** @type {?Source} */
    source;
    /** @type {?string[]} */
    mailingLists;
    /** @type {?string} */
    externalRef;
    /** @type {?number} */
    appUserId;
    /** @type {?string} */
    createdDate;
    /** @type {?string} */
    verifiedDate;
    /** @type {?string} */
    deletedDate;
    /** @type {?string} */
    unsubscribedDate;
    getTypeName() { return 'UpdateContact' }
    getMethod() { return 'PATCH' }
    createResponse() { return new Contact() }
}
export class DeleteContact {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteContact' }
    getMethod() { return 'DELETE' }
    createResponse() { }
}
export class UpdateMailMessage {
    /** @param {{id?:number,email?:string,layout?:string,template?:string,renderer?:string,rendererArgs?:{ [index: string]: Object; },message?:EmailMessage,completedDate?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    email;
    /** @type {?string} */
    layout;
    /** @type {?string} */
    template;
    /** @type {?string} */
    renderer;
    /** @type {?{ [index: string]: Object; }} */
    rendererArgs;
    /** @type {?EmailMessage} */
    message;
    /** @type {?string} */
    completedDate;
    getTypeName() { return 'UpdateMailMessage' }
    getMethod() { return 'PATCH' }
    createResponse() { return new MailMessage() }
}
export class DeleteMailMessages {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteMailMessages' }
    getMethod() { return 'DELETE' }
    createResponse() { }
}
export class CreateMailRun {
    /** @param {{mailingList?:MailingList,layout?:string,template?:string,generator?:string,generatorArgs?:{ [index: string]: Object; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {MailingList} */
    mailingList;
    /** @type {string} */
    layout;
    /** @type {string} */
    template;
    /** @type {string} */
    generator;
    /** @type {{ [index: string]: Object; }} */
    generatorArgs;
    getTypeName() { return 'CreateMailRun' }
    getMethod() { return 'POST' }
    createResponse() { return new MailRun() }
}
export class UpdateMailRun {
    /** @param {{id?:number,mailingList?:MailingList,layout?:string,template?:string,generator?:string,generatorArgs?:{ [index: string]: Object; },createdDate?:string,generatedDate?:string,sentDate?:string,completedDate?:string,emailsCount?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?MailingList} */
    mailingList;
    /** @type {?string} */
    layout;
    /** @type {?string} */
    template;
    /** @type {?string} */
    generator;
    /** @type {?{ [index: string]: Object; }} */
    generatorArgs;
    /** @type {string} */
    createdDate;
    /** @type {?string} */
    generatedDate;
    /** @type {?string} */
    sentDate;
    /** @type {?string} */
    completedDate;
    /** @type {?number} */
    emailsCount;
    getTypeName() { return 'UpdateMailRun' }
    getMethod() { return 'PUT' }
    createResponse() { return new MailRun() }
}
export class DeleteMailRun {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteMailRun' }
    getMethod() { return 'DELETE' }
    createResponse() { }
}
export class UpdateMailRunMessage {
    /** @param {{id?:number,mailRunId?:number,contactId?:number,renderer?:string,rendererArgs?:{ [index: string]: Object; },message?:EmailMessage,startedDate?:string,completedDate?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    mailRunId;
    /** @type {number} */
    contactId;
    /** @type {string} */
    renderer;
    /** @type {{ [index: string]: Object; }} */
    rendererArgs;
    /** @type {?EmailMessage} */
    message;
    /** @type {?string} */
    startedDate;
    /** @type {?string} */
    completedDate;
    getTypeName() { return 'UpdateMailRunMessage' }
    getMethod() { return 'PATCH' }
    createResponse() { return new MailMessageRun() }
}
export class DeleteMailRunMessage {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteMailRunMessage' }
    getMethod() { return 'DELETE' }
    createResponse() { }
}
export class Authenticate {
    /** @param {{provider?:string,userName?:string,password?:string,rememberMe?:boolean,accessToken?:string,accessTokenSecret?:string,returnUrl?:string,errorView?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {string}
     * @description AuthProvider, e.g. credentials */
    provider;
    /** @type {string} */
    userName;
    /** @type {string} */
    password;
    /** @type {?boolean} */
    rememberMe;
    /** @type {string} */
    accessToken;
    /** @type {string} */
    accessTokenSecret;
    /** @type {string} */
    returnUrl;
    /** @type {string} */
    errorView;
    /** @type {{ [index: string]: string; }} */
    meta;
    getTypeName() { return 'Authenticate' }
    getMethod() { return 'POST' }
    createResponse() { return new AuthenticateResponse() }
}

