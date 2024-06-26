﻿using ServiceStack;
using ServiceStack.DataAnnotations;
using CreatorKit.ServiceModel.Types;

namespace CreatorKit.ServiceModel;

[Renderer<RenderSimpleText>]
[Tag(Tag.Mail), ValidateIsAdmin]
[Description("Simple Text Email")]
public class SimpleTextEmail : CreateEmailBase, IPost, IReturn<MailMessage>
{
    [ValidateNotEmpty]
    [FieldCss(Field = "col-span-12")]
    public string Subject { get; set; }

    [ValidateNotEmpty]
    [Input(Type = "textarea"), FieldCss(Field = "col-span-12", Input = "h-36")]
    public string Body { get; set; }
    public bool? Draft { get; set; }
}

[Renderer<RenderCustomHtml>]
[Tag(Tag.Mail), ValidateIsAdmin]
[Icon(Svg = Icons.RichHtml)]
[Description("Custom HTML Email")]
public class CustomHtmlEmail : CreateEmailBase, IPost, IReturn<MailMessage>
{
    [ValidateNotEmpty]
    [Input(Type = "combobox", EvalAllowableValues = "AppData.EmailLayoutOptions")]
    public string Layout { get; set; }
    
    [ValidateNotEmpty]
    [Input(Type = "combobox", EvalAllowableValues = "AppData.EmailTemplateOptions")]
    public string Template { get; set; }
    
    [ValidateNotEmpty]
    [FieldCss(Field = "col-span-12")]
    public string Subject { get; set; }

    [Input(Type = "MarkdownEmailInput", Label = ""), FieldCss(Field = "col-span-12", Input = "h-56")]
    public string? Body { get; set; }
    public bool? Draft { get; set; }
}

[Renderer<RenderTagQuestionsEmail>]
[Tag(ServiceModel.Tag.Mail), ValidateIsAdmin]
[Description("New Questions with Tag")]
public class TagQuestionsEmail : CreateEmailBase, IPost, IReturn<MailMessage>
{
    [ValidateNotEmpty]
    public string Tag { get; set; }
    
    [ValidateNotEmpty]
    public DateTime Date { get; set; }

    public bool? Draft { get; set; }
}

