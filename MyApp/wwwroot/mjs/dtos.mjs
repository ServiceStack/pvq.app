/* Options:
Date: 2024-03-29 23:35:25
Version: 8.22
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
export class Post {
    /** @param {{id?:number,postTypeId?:number,acceptedAnswerId?:number,parentId?:number,score?:number,viewCount?:number,title?:string,favoriteCount?:number,creationDate?:string,lastActivityDate?:string,lastEditDate?:string,lastEditorUserId?:number,ownerUserId?:number,tags?:string[],slug?:string,summary?:string,rankDate?:string,answerCount?:number,createdBy?:string,modifiedBy?:string,refId?:string,body?:string,modifiedReason?:string,lockedDate?:string,lockedReason?:string}} [init] */
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
    refId;
    /** @type {?string} */
    body;
    /** @type {?string} */
    modifiedReason;
    /** @type {?string} */
    lockedDate;
    /** @type {?string} */
    lockedReason;
}
export class StatTotals {
    /** @param {{id?:string,postId?:number,createdBy?:string,favoriteCount?:number,viewCount?:number,upVotes?:number,downVotes?:number,startingUpVotes?:number}} [init] */
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
}
export class Meta {
    /** @param {{id?:number,modelVotes?:{ [index: string]: number; },comments?:{ [index: string]: Comment[]; },statTotals?:StatTotals[],modifiedDate?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {{ [index: string]: number; }} */
    modelVotes;
    /** @type {{ [index: string]: Comment[]; }} */
    comments;
    /** @type {StatTotals[]} */
    statTotals;
    /** @type {string} */
    modifiedDate;
}
export class ChoiceMessage {
    /** @param {{role?:string,content?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    role;
    /** @type {string} */
    content;
}
export class Choice {
    /** @param {{index?:number,message?:ChoiceMessage}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    index;
    /** @type {ChoiceMessage} */
    message;
}
export class Comment {
    /** @param {{body?:string,created?:number,createdBy?:string,upVotes?:number,reports?:number}} [init] */
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
}
export class Answer {
    /** @param {{id?:string,object?:string,created?:number,model?:string,choices?:Choice[],usage?:{ [index: string]: number; },temperature?:number,comments?:Comment[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    /** @type {string} */
    object;
    /** @type {number} */
    created;
    /** @type {string} */
    model;
    /** @type {Choice[]} */
    choices;
    /** @type {{ [index: string]: number; }} */
    usage;
    /** @type {number} */
    temperature;
    /** @type {Comment[]} */
    comments;
}
export class QuestionAndAnswers {
    /** @param {{id?:number,post?:Post,meta?:Meta,answers?:Answer[],viewCount?:number,questionScore?:number,questionComments?:Comment[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {Post} */
    post;
    /** @type {?Meta} */
    meta;
    /** @type {Answer[]} */
    answers;
    /** @type {number} */
    viewCount;
    /** @type {number} */
    questionScore;
    /** @type {Comment[]} */
    questionComments;
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
export class ModelTotalScore {
    /** @param {{id?:string,totalScore?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    /** @type {number} */
    totalScore;
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
    /** @param {{id?:string,winRate?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    /** @type {number} */
    winRate;
}
export class ModelWinRateByTag {
    /** @param {{id?:string,tag?:string,winRate?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    /** @type {string} */
    tag;
    /** @type {number} */
    winRate;
}
export class ModelTotalScoreByTag {
    /** @param {{id?:string,tag?:string,totalScore?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    /** @type {string} */
    tag;
    /** @type {number} */
    totalScore;
}
export class ModelWinRate {
    /** @param {{id?:string,winRate?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    /** @type {number} */
    winRate;
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
    /** @param {{mostLikedModels?:ModelTotalScore[],mostLikedModelsByLlm?:ModelTotalStartUpVotes[],answererWinRate?:LeaderBoardWinRate[],humanVsLlmWinRateByHumanVotes?:LeaderBoardWinRate[],humanVsLlmWinRateByLlmVotes?:LeaderBoardWinRate[],modelWinRateByTag?:ModelWinRateByTag[],modelTotalScore?:ModelTotalScore[],modelTotalScoreByTag?:ModelTotalScoreByTag[],modelWinRate?:ModelWinRate[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {ModelTotalScore[]} */
    mostLikedModels;
    /** @type {ModelTotalStartUpVotes[]} */
    mostLikedModelsByLlm;
    /** @type {LeaderBoardWinRate[]} */
    answererWinRate;
    /** @type {LeaderBoardWinRate[]} */
    humanVsLlmWinRateByHumanVotes;
    /** @type {LeaderBoardWinRate[]} */
    humanVsLlmWinRateByLlmVotes;
    /** @type {ModelWinRateByTag[]} */
    modelWinRateByTag;
    /** @type {ModelTotalScore[]} */
    modelTotalScore;
    /** @type {ModelTotalScoreByTag[]} */
    modelTotalScoreByTag;
    /** @type {ModelWinRate[]} */
    modelWinRate;
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
export class IdResponse {
    /** @param {{id?:string,responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    /** @type {ResponseStatus} */
    responseStatus;
}
export class CommentsResponse {
    /** @param {{comments?:Comment[],responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {Comment[]} */
    comments;
    /** @type {ResponseStatus} */
    responseStatus;
}
export class GetUserReputationsResponse {
    /** @param {{results?:{ [index: string]: number; },responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {{ [index: string]: number; }} */
    results;
    /** @type {?ResponseStatus} */
    responseStatus;
}
export class UpdateUserProfileResponse {
    /** @param {{responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {ResponseStatus} */
    responseStatus;
}
export class UserPostDataResponse {
    /** @param {{upVoteIds?:string[],downVoteIds?:string[],responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string[]} */
    upVoteIds;
    /** @type {string[]} */
    downVoteIds;
    /** @type {?ResponseStatus} */
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
    constructor(init) { Object.assign(this, init) }
    getTypeName() { return 'CalculateLeaderBoard' }
    getMethod() { return 'GET' }
    createResponse() { return new CalculateLeaderboardResponse() }
}
export class AskQuestion {
    /** @param {{title?:string,body?:string,tags?:string[],refId?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    title;
    /** @type {string} */
    body;
    /** @type {string[]} */
    tags;
    /** @type {?string} */
    refId;
    getTypeName() { return 'AskQuestion' }
    getMethod() { return 'POST' }
    createResponse() { return new AskQuestionResponse() }
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
    /** @param {{postId?:number,body?:string,refId?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    postId;
    /** @type {string} */
    body;
    /** @type {?string} */
    refId;
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
export class GetAnswerBody {
    /** @param {{id?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    getTypeName() { return 'GetAnswerBody' }
    getMethod() { return 'GET' }
    createResponse() { return '' }
}
export class CreateWorkerAnswer {
    /** @param {{postId?:number,model?:string,json?:string,postJobId?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    postId;
    /** @type {string} */
    model;
    /** @type {string} */
    json;
    /** @type {?number} */
    postJobId;
    getTypeName() { return 'CreateWorkerAnswer' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
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
export class UpdateUserProfile {
    constructor(init) { Object.assign(this, init) }
    getTypeName() { return 'UpdateUserProfile' }
    getMethod() { return 'POST' }
    createResponse() { return new UpdateUserProfileResponse() }
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
export class RenderComponent {
    /** @param {{ifQuestionModified?:number,regenerateMeta?:number,question?:QuestionAndAnswers,home?:RenderHome}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?number} */
    ifQuestionModified;
    /** @type {?number} */
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
export class QueryPosts extends QueryDb {
    /** @param {{skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    getTypeName() { return 'QueryPosts' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}

