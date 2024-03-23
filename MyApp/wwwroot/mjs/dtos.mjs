/* Options:
Date: 2024-03-23 01:13:02
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
    /** @param {{id?:number,postTypeId?:number,acceptedAnswerId?:number,parentId?:number,score?:number,viewCount?:number,title?:string,favoriteCount?:number,creationDate?:string,lastActivityDate?:string,lastEditDate?:string,lastEditorUserId?:number,ownerUserId?:number,tags?:string[],slug?:string,summary?:string,rankDate?:string,answerCount?:number,body?:string}} [init] */
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
    body;
}
export class StatTotals {
    /** @param {{id?:string,postId?:number,favoriteCount?:number,viewCount?:number,upVotes?:number,downVotes?:number,startingUpVotes?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    /** @type {number} */
    postId;
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
export class Comment {
    /** @param {{id?:string,body?:string,createdBy?:string,upVotes?:number,createdDate?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    /** @type {string} */
    body;
    /** @type {string} */
    createdBy;
    /** @type {?number} */
    upVotes;
    /** @type {string} */
    createdDate;
}
export class AnswerMeta {
    /** @param {{comments?:Comment[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {Comment[]} */
    comments;
}
export class Meta {
    /** @param {{id?:number,statTotals?:StatTotals[],modelVotes?:{ [index: string]: number; },comments?:Comment[],answers?:{ [index: string]: AnswerMeta; },modifiedDate?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {StatTotals[]} */
    statTotals;
    /** @type {{ [index: string]: number; }} */
    modelVotes;
    /** @type {Comment[]} */
    comments;
    /** @type {{ [index: string]: AnswerMeta; }} */
    answers;
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
export class Answer {
    /** @param {{id?:string,object?:string,created?:number,model?:string,choices?:Choice[],usage?:{ [index: string]: number; },temperature?:number,comments?:Comment[],votes?:number,upVotes?:number,downVotes?:number}} [init] */
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
    /** @type {number} */
    votes;
    /** @type {number} */
    upVotes;
    /** @type {number} */
    downVotes;
}
export class QuestionAndAnswers {
    /** @param {{id?:number,post?:Post,meta?:Meta,statTotals?:StatTotals,answers?:Answer[],viewCount?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {Post} */
    post;
    /** @type {?Meta} */
    meta;
    /** @type {StatTotals} */
    statTotals;
    /** @type {Answer[]} */
    answers;
    /** @type {number} */
    viewCount;
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
    /** @param {{jobId?:number,postId?:number,title?:string,body?:string,tags?:string[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    jobId;
    /** @type {number} */
    postId;
    /** @type {string} */
    title;
    /** @type {string} */
    body;
    /** @type {string[]} */
    tags;
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
export class CheckPostJobsResponse {
    /** @param {{results?:PostJob[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {PostJob[]} */
    results;
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
export class CheckPostJobs {
    /** @param {{workerId?:string,models?:string[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    workerId;
    /** @type {string[]} */
    models;
    getTypeName() { return 'CheckPostJobs' }
    getMethod() { return 'GET' }
    createResponse() { return new CheckPostJobsResponse() }
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

