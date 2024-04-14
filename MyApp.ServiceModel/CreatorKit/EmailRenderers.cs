﻿using System.Collections.Generic;
using ServiceStack;
using ServiceStack.DataAnnotations;

namespace CreatorKit.ServiceModel;

[Tag(Tag.Mail), ValidateIsAdmin]
public class PreviewEmail : IPost, IReturn<string>
{
    public string? Request { get; set; }
    public string? Renderer { get; set; }
    [ValidateNotNull]
    public Dictionary<string,object> RequestArgs { get; set; }
}

[Tag(Tag.Mail), ValidateIsAdmin, ExcludeMetadata]
public class RenderSimpleText : RenderEmailBase, IGet, IReturn<string>
{
    public string Body { get; set; }
}

[Tag(Tag.Mail), ValidateIsAdmin, ExcludeMetadata]
public class RenderCustomHtml : RenderEmailBase, IGet, IReturn<string>
{
    [ValidateNotEmpty]
    [Input(Type = "combobox", EvalAllowableValues = "AppData.EmailLayoutOptions")]
    public string Layout { get; set; }

    [ValidateNotEmpty]
    [Input(Type = "combobox", EvalAllowableValues = "AppData.EmailTemplateOptions")]
    public string Template { get; set; }
    
    public string Body { get; set; }
}

[Route("/docs/{Page}")]
public class RenderDoc : IGet, IReturn<string>
{
    [ValidateNotEmpty]
    public string Page { get; set; }
}