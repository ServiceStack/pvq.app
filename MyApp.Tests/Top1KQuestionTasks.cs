using MyApp.Data;
using MyApp.Migrations;
using MyApp.ServiceModel;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.Text;

namespace MyApp.Tests;

[Explicit]
public class Top1KQuestionTasks
{
    [Test]
    public async Task Find_Missing_Top1K_Questions_For_Model()
    {
        var model = "mixtral";

        var client = TestUtils.CreateProdClient();
        
        var api = await client.ApiAsync(new MissingTop1K
        {
            Model = model,
        });
        
        api.Response.PrintDump();
    }

    [Test]
    public async Task Create_missing_Top1K_Answers_for_Models()
    {
        var client = TestUtils.CreateProdClient();
        await client.ApiAsync(new Authenticate
        {
            provider = "credentials",
            UserName = "mythz",
            Password = Environment.GetEnvironmentVariable("AUTH_SECRET")
        });

        var allModels = AppConfig.ModelsForQuestions.Select(x => x.Model).ToList();
        allModels.Remove("deepseek-coder-33b");
        
        allModels.PrintDump();
        
        foreach (var model in allModels)
        {
            await CreateMissing1KModelsForModelAsync(client, model);
        }
    }

    [Test]
    public async Task Create_missing_Top1K_Answers_for_Adhoc_Model()
    {
        var model = "phi4";

        var client = await TestUtils.CreateAuthenticatedProdClientAsync();
        await CreateMissing1KModelsForModelAsync(client, model);
    }

    private static async Task CreateMissing1KModelsForModelAsync(JsonApiClient client, string model)
    {
        var api = await client.ApiAsync(new MissingTop1K
        {
            Model = model,
        });
            
        api.Error.PrintDump();
        api.ThrowIfError();
        api.Response.PrintDump();

        if (api.Response!.Results.Count == 0)
        {
            $"No more missing questions for {model}".Print();
            return;
        }

        var apiCreate = await client.ApiAsync(new CreateAnswersForModels
        {
            Models = [model],
            PostIds = api.Response!.Results,
        });
            
        apiCreate.Error.PrintDump();
        apiCreate.ThrowIfError();
        apiCreate.Response!.Errors.PrintDump();
        apiCreate.Response!.Results.PrintDump();
    }

    [Test]
    public async Task Recreate_answers_for_Top1K_questions_for_phi()
    {
        var client = await TestUtils.CreateAuthenticatedProdClientAsync();
        var apiCreate = await client.ApiAsync(new CreateAnswersForModels
        {
            Models = ["phi"],
            PostIds = Migration1005.Top1KIds,
        });

        apiCreate.Error.PrintDump();
        apiCreate.ThrowIfError();
        apiCreate.Response!.Errors.PrintDump();
        apiCreate.Response!.Results.PrintDump();;
    }

    [Test]
    public async Task Recreate_answers_for_Top1K_questions_for_gemini_pro_15()
    {
        var client = await TestUtils.CreateAuthenticatedProdClientAsync();
        var apiCreate = await client.ApiAsync(new CreateAnswersForModels
        {
            Models = ["gemini-pro-1.5"],
            PostIds = Migration1005.Top1KIds,
        });

        apiCreate.Error.PrintDump();
        apiCreate.ThrowIfError();
        apiCreate.Response!.Errors.PrintDump();
        apiCreate.Response!.Results.PrintDump();;
    }

    [Test]
    public async Task Recreate_answers_for_Top1K_questions_for_gemini_flash()
    {
        var client = await TestUtils.CreateAuthenticatedProdClientAsync();
        var apiCreate = await client.ApiAsync(new CreateAnswersForModels
        {
            Models = ["gemini-flash"],
            PostIds = Migration1005.Top1KIds,
        });

        apiCreate.Error.PrintDump();
        apiCreate.ThrowIfError();
        apiCreate.Response!.Errors.PrintDump();
        apiCreate.Response!.Results.PrintDump();;
    }

    [Test]
    public async Task Recreate_answers_for_Top1K_questions_for_gemma2()
    {
        var client = await TestUtils.CreateAuthenticatedProdClientAsync();
        var apiCreate = await client.ApiAsync(new CreateAnswersForModels
        {
            Models = ["gemma2:27b"],
            PostIds = Migration1005.Top1KIds,
        });

        apiCreate.Error.PrintDump();
        apiCreate.ThrowIfError();
        apiCreate.Response!.Errors.PrintDump();
        apiCreate.Response!.Results.PrintDump();;
    }

    [Test]
    public async Task Recreate_answers_for_Top1K_questions_for_sonnet3_5()
    {
        var client = await TestUtils.CreateAuthenticatedProdClientAsync();
        var apiCreate = await client.ApiAsync(new CreateAnswersForModels
        {
            Models = ["claude-3-5-sonnet"],
            PostIds = Migration1005.Top1KIds,
        });

        apiCreate.Error.PrintDump();
        apiCreate.ThrowIfError();
        apiCreate.Response!.Errors.PrintDump();
        apiCreate.Response!.Results.PrintDump();;
    }

    [Test]
    public async Task Recreate_answers_for_Top1K_questions_for_gpt4o_mini()
    {
        var client = await TestUtils.CreateAuthenticatedProdClientAsync();
        var apiCreate = await client.ApiAsync(new CreateAnswersForModels
        {
            Models = ["gpt-4o-mini"],
            PostIds = Migration1005.Top1KIds,
        });

        apiCreate.Error.PrintDump();
        apiCreate.ThrowIfError();
        apiCreate.Response!.Errors.PrintDump();
        apiCreate.Response!.Results.PrintDump();;
    }

    [Test]
    public async Task Recreate_answers_for_Top1K_questions_for_Mistral_Nemo()
    {
        // var client = await TestUtils.CreateAuthenticatedProdClientAsync();
        var client = await TestUtils.CreateAuthenticatedDevClientAsync();
        var apiCreate = await client.ApiAsync(new CreateAnswersForModels
        {
            Models = ["mistral-nemo"],
            PostIds = [9],
            // PostIds = Migration1005.Top1KIds,
        });

        apiCreate.Error.PrintDump();
        apiCreate.ThrowIfError();
        apiCreate.Response!.Errors.PrintDump();
        apiCreate.Response!.Results.PrintDump();;
    }

    [Test]
    public async Task Recreate_answers_for_Top1K_questions_for_DeepSeekCoderV2()
    {
        var client = await TestUtils.CreateAuthenticatedProdClientAsync();
        // var client = await TestUtils.CreateAuthenticatedDevClientAsync();
        var apiCreate = await client.ApiAsync(new CreateAnswersForModels
        {
            Models = ["deepseek-coder2-236b"],
            // PostIds = [9],
            PostIds = Migration1005.Top1KIds,
        });

        apiCreate.Error.PrintDump();
        apiCreate.ThrowIfError();
        apiCreate.Response!.Errors.PrintDump();
        apiCreate.Response!.Results.PrintDump();;
    }

    [Test]
    public async Task Recreate_answers_for_Top1K_questions_for_DeepSeekV3()
    {
        var client = await TestUtils.CreateAuthenticatedProdClientAsync();
        // var client = await TestUtils.CreateAuthenticatedDevClientAsync();
        var apiCreate = await client.ApiAsync(new CreateAnswersForModels
        {
            Models = ["deepseek-v3:671b"],
            PostIds = [9],
            // PostIds = Migration1005.Top1KIds,
        });

        apiCreate.Error.PrintDump();
        apiCreate.ThrowIfError();
        apiCreate.Response!.Errors.PrintDump();
        apiCreate.Response!.Results.PrintDump();;
    }
    
    [Test]
    public async Task Recreate_answers_for_Top1K_questions_for_Phi4()
    {
        var client = await TestUtils.CreateAuthenticatedProdClientAsync();
        // var client = await TestUtils.CreateAuthenticatedDevClientAsync();
        var apiCreate = await client.ApiAsync(new CreateAnswersForModels
        {
            Models = ["phi4"],
            // PostIds = [9],
            PostIds = Migration1005.Top1KIds,
        });

        apiCreate.Error.PrintDump();
        apiCreate.ThrowIfError();
        apiCreate.Response!.Errors.PrintDump();
        apiCreate.Response!.Results.PrintDump();;
    }
    
    [Test]
    public async Task Recreate_answers_for_Top1K_questions_for_Llama_3_1()
    {
        var client = await TestUtils.CreateAuthenticatedProdClientAsync();
        // var client = await TestUtils.CreateAuthenticatedDevClientAsync();
        var apiCreate = await client.ApiAsync(new CreateAnswersForModels
        {
            Models = ["llama3.1-8b"],
            // PostIds = [9],
            PostIds = Migration1005.Top1KIds,
        });

        apiCreate.Error.PrintDump();
        apiCreate.ThrowIfError();
        apiCreate.Response!.Errors.PrintDump();
        apiCreate.Response!.Results.PrintDump();;
    }

    [Test]
    public async Task Find_answers_that_have_not_been_individually_graded()
    {
        var client = await TestUtils.CreateAuthenticatedProdClientAsync();
        // var client = await TestUtils.CreateAuthenticatedDevClientAsync();
        
        var api = await client.ApiAsync(new MissingGradedAnswersTop1K());
            
        api.Error.PrintDump();
        api.ThrowIfError();

        if (api.Response!.Results.Count == 0)
        {
            "No more ungraded answers".Print();
            return;
        }

        api.Response!.Results.RemoveAll(x => !x.EndsWith("-phi4"));
        api.Response.PrintDump();

        var apiCreate = await client.ApiAsync(new CreateRankingTasks
        {
            AnswerIds = api.Response!.Results,
        });
            
        apiCreate.Error.PrintDump();
        apiCreate.ThrowIfError();
        apiCreate.Response!.Errors.PrintDump();
        apiCreate.Response!.Results.PrintDump();
    }

    [Test]
    public async Task Create_Mistral_Nemo_User()
    {
        // var client = await TestUtils.CreateAdminDevClientAsync();
        var client = await TestUtils.CreateAdminProdClientAsync();
        var api = await client.ApiAsync(new AdminCreateUser
        {
            UserName = "mistral-nemo",
            DisplayName = "Mistral NeMo",
            Email = "servicestack.mail+mistral-nemo@gmail.com",
            UserAuthProperties = new()
            {
                [nameof(ApplicationUser.Model)] = "mistral-nemo",
                [nameof(ApplicationUser.DisplayName)] = "Mistral NeMo",
                [nameof(ApplicationUser.ProfilePath)] = "/profiles/mi/mistral-nemo/mistral-nemo.svg",
            },
            Password = Environment.GetEnvironmentVariable("AUTH_SECRET"),
        });
        api.Response.PrintDump();
        api.ThrowIfError();
    }

    [Test]
    public async Task Create_DeepSeek236B_User()
    {
        // var client = await TestUtils.CreateAdminDevClientAsync();
        var client = await TestUtils.CreateAdminProdClientAsync();
        var api = await client.ApiAsync(new AdminCreateUser
        {
            UserName = "deepseek-coder2-236b",
            DisplayName = "DeepSeek Coder2 236B",
            Email = "servicestack.mail+deepseek-coder2-236b@gmail.com",
            UserAuthProperties = new()
            {
                [nameof(ApplicationUser.Model)] = "deepseek-coder-v2:236b",
                [nameof(ApplicationUser.DisplayName)] = "DeepSeek Coder2 236B",
                [nameof(ApplicationUser.ProfilePath)] = "/profiles/de/deepseek-coder2-236b/deepseek-coder2-236b.jpg",
            },
            Password = Environment.GetEnvironmentVariable("AUTH_SECRET"),
        });
        api.Response.PrintDump();
        api.ThrowIfError();
    }

    [Test]
    public async Task Create_Claude37_Sonnet_User()
    {
        var client = await TestUtils.CreateAdminDevClientAsync();
        // var client = await TestUtils.CreateAdminProdClientAsync();
        var api = await client.ApiAsync(new AdminCreateUser
        {
            UserName = "claude3-7-sonnet",
            Email = "servicestack.mail+claude3-7-sonnet@gmail.com",
            DisplayName = "Claude 3.7 Sonnet",
            UserAuthProperties = new()
            {
                [nameof(ApplicationUser.Model)] = "claude-3-7-sonnet",
                [nameof(ApplicationUser.DisplayName)] = "Claude 3.7 Sonnet",
                [nameof(ApplicationUser.ProfilePath)] = "/profiles/cl/claude3-sonnet/claude3-sonnet.svg",
            },
            Password = Environment.GetEnvironmentVariable("AUTH_SECRET"),
        });
        api.Response.PrintDump();
        api.ThrowIfError();
    }

    [Test]
    public async Task Create_Llama31_User()
    {
        var client = await TestUtils.CreateAdminDevClientAsync();
        // var client = await TestUtils.CreateAdminProdClientAsync();
        var api = await client.ApiAsync(new AdminCreateUser
        {
            UserName = "llama3.1-8b",
            Email = "servicestack.mail+llama3.1-8b@gmail.com",
            UserAuthProperties = new()
            {
                [nameof(ApplicationUser.Model)] = "llama3.1:8b",
                [nameof(ApplicationUser.DisplayName)] = "Llama 3.1 8B",
                [nameof(ApplicationUser.ProfilePath)] = "/profiles/ll/llama3.1-8b/llama3.1-8b.svg",
            },
            Password = Environment.GetEnvironmentVariable("AUTH_SECRET"),
        });
        api.Response.PrintDump();
        api.ThrowIfError();
    }

    [Test]
    public async Task Create_Llama4_109b_User()
    {
        var client = await TestUtils.CreateAdminDevClientAsync();
        // var client = await TestUtils.CreateAdminProdClientAsync();
        var api = await client.ApiAsync(new AdminCreateUser
        {
            UserName = "llama4-109b",
            Email = "servicestack.mail+llama4-109b@gmail.com",
            UserAuthProperties = new()
            {
                [nameof(ApplicationUser.Model)] = "llama4:109b",
                [nameof(ApplicationUser.DisplayName)] = "Llama 4 Scout 109B",
                [nameof(ApplicationUser.ProfilePath)] = "/profiles/ll/llama/llama.svg",
            },
            Password = Environment.GetEnvironmentVariable("AUTH_SECRET"),
        });
        api.Response.PrintDump();
        api.ThrowIfError();
    }

    [Test]
    public async Task Create_Llama4_400b_User()
    {
        // var client = await TestUtils.CreateAdminDevClientAsync();
        var client = await TestUtils.CreateAdminProdClientAsync();
        var api = await client.ApiAsync(new AdminCreateUser
        {
            UserName = "llama4-400b",
            Email = "servicestack.mail+llama4-400b@gmail.com",
            UserAuthProperties = new()
            {
                [nameof(ApplicationUser.Model)] = "llama4:400b",
                [nameof(ApplicationUser.DisplayName)] = "Llama 4 Maverick 400B",
                [nameof(ApplicationUser.ProfilePath)] = "/profiles/ll/llama/llama.svg",
            },
            Password = Environment.GetEnvironmentVariable("AUTH_SECRET"),
        });
        api.Response.PrintDump();
        api.ThrowIfError();
    }

    [Test]
    public async Task Create_Gemini3_27b_User()
    {
        // var client = await TestUtils.CreateAdminDevClientAsync();
        var client = await TestUtils.CreateAdminProdClientAsync();
        var api = await client.ApiAsync(new AdminCreateUser
        {
            UserName = "gemma3-27b",
            Email = "servicestack.mail+gemma3-27b@gmail.com",
            UserAuthProperties = new()
            {
                [nameof(ApplicationUser.Model)] = "gemma3:27b",
                [nameof(ApplicationUser.DisplayName)] = "Gemma3 27B",
                [nameof(ApplicationUser.ProfilePath)] = "/profiles/ge/gemma/gemma.svg",
            },
            Password = Environment.GetEnvironmentVariable("AUTH_SECRET"),
        });
        api.Response.PrintDump();
        api.ThrowIfError();
    }

    [Test]
    public async Task Generate_top_10k_answers_for_gemini_flash()
    {
        var txt = await File.ReadAllTextAsync(TestUtils.GetHostDir().CombineWith("App_Data/top10k-10.txt"));
        var ids = new List<int>();
        foreach (var line in txt.ReadLines())
        {
            ids.Add(int.Parse(line.Trim()));
        }
        
        var client = await TestUtils.CreateAuthenticatedProdClientAsync();
        // client.GetHttpClient().Timeout = TimeSpan.FromMinutes(5);
        var apiCreate = await client.ApiAsync(new CreateAnswersForModels
        {
            // Models = ["gemini-flash","gemini-pro-1.5"],
            Models = ["gemini-flash"],
            PostIds = ids,
        });

        apiCreate.Error.PrintDump();
        apiCreate.ThrowIfError();
        apiCreate.Response!.Errors.PrintDump();
        apiCreate.Response!.Results.PrintDump();;
    }
}