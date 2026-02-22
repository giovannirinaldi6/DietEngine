using DietWorker.DTO;
using DietWorker.Models;
using Microsoft.EntityFrameworkCore;
using OpenAI;
using OpenAI.Chat;
using System.Text;
using System.Text.Json;
using OpenAI.Models;
using Org.BouncyCastle.Ocsp;
using Microsoft.Extensions.Options;


namespace DietWorker.Services
{
    public class DietService : IDietService
    {
        private readonly AppDbContext dbContext;
        private readonly ChatClient _chatClient;
        private readonly EmailService _emailService;
        private readonly PersoneOptions _personeOptions;
        public DietService(AppDbContext context, ChatClient chatClient, EmailService emailService, IOptions<PersoneOptions> personeOptions)
        {
            dbContext = context;
            _chatClient = chatClient;
            _emailService = emailService;
            _personeOptions = personeOptions.Value;
        }

        public async Task<bool> RunDailyRecommendationAsync()
        {
            //prendere mail dal'app setting
            string? menu = await _emailService.GetTodayMenusFromEmailAsync(_personeOptions.MenuFrom);

            DateTime oggi = DateTime.Today;
            int deltaLunedi = DayOfWeek.Monday - oggi.DayOfWeek;
            if (deltaLunedi > 0) deltaLunedi -= 7; // se oggi è domenica, torna indietro di 6 giorni
            DateTime lunediCorrente = oggi.AddDays(deltaLunedi);

            // Recupera i pasti dalla tabella dal lunedì della settimana corrente
            string lunediString = lunediCorrente.ToString("yyyy-MM-dd");

            var lastMeals = await dbContext.MealHistories
                .Where(m => string.Compare(m.Date, lunediString) >= 0)
                .OrderBy(m => m.Date)
                .ToArrayAsync();

            // Controlla se negli ultimi 4 giorni c'è stato un dolce
            bool dolceNellaSettimana = lastMeals
                .Any(m => m.TipologiaPiatto == "Dolce");

            var forbidden = await dbContext.PastiNonConsentitis
                .Select(p => p.Name)
                .ToArrayAsync();

            Random rnd = new Random();
            bool aggiungiDolce;
            if (!dolceNellaSettimana)
            {
                // Calcolo il flag randomico al 30%
                aggiungiDolce = rnd.NextDouble() < 0.30;
            }
            else
            {
                // Già mangiato nella settimana
                aggiungiDolce = false;
            }

            if (menu != null)
            {
                var prompt = BuildNutritionPrompt(
                    menuText: menu,               // testo del menu preso dalla mail
                    forbiddenIngredients: forbidden, // array di ingredienti da evitare
                    last5Meals: lastMeals,       // storico ultimi 5 pasti
                    aggiungiDolce
                );

                var recommendations = await GetAiRecommendationAsync(prompt);

                if (recommendations != null)
                {
                    foreach (var rec in recommendations)
                    {
                        Console.WriteLine($"Piatto scelto: {rec.Piatto_scelto}");
                        Console.WriteLine($"Tipologia: {rec.Tipologia_piatto}");
                        Console.WriteLine($"Motivazione: {rec.Motivazione}");
                        Console.WriteLine($"Livello equilibrio: {rec.Livello_equilibrio}");

                        
                    }
                    await SaveRecommendationsAsync(recommendations);

                    await _emailService.SendSelectedDishesAsync(_personeOptions.MenuTo, recommendations);
                }
            }



            return true;
        }

        public async Task SaveRecommendationsAsync(List<MealRecommendationDTO> recommendations)
        {
            var todayString = DateTime.Today.ToString("yyyy-MM-dd");

            foreach (var recommendation in recommendations)
            {
                // Controlla se esiste già un record per lo stesso piatto e data
                var existing = await dbContext.MealHistories
                    .FirstOrDefaultAsync(x => x.Date == todayString && x.DishName == recommendation.Piatto_scelto);

                if (existing != null && false)
                {
                    // Aggiorna record esistente
                    existing.ProteinCategory = recommendation.Categoria_proteina;
                    existing.TipologiaPiatto = recommendation.Tipologia_piatto;
                    existing.VarietyScore = recommendation.Livello_equilibrio;
                    existing.CarbCategory = null;
                    existing.CookingType = null;
                }
                else
                {
                    // Crea nuovo record
                    var entity = new MealHistory
                    {
                        Date = todayString,
                        DishName = recommendation.Piatto_scelto,
                        ProteinCategory = recommendation.Categoria_proteina,
                        TipologiaPiatto = recommendation.Tipologia_piatto,
                        VarietyScore = recommendation.Livello_equilibrio,
                        CarbCategory = null,
                        CookingType = null
                    };

                    dbContext.MealHistories.Add(entity);
                }
            }

            await dbContext.SaveChangesAsync();
        }

        public async Task<List<MealRecommendationDTO>?> GetAiRecommendationAsync(string prompt)
        {
            ChatCompletionOptions options = new()
            {
                ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: "meal_recommendation",
                jsonSchema: BinaryData.FromBytes("""
                    {
                      "type": "object",
                      "properties": {
                        "scelte": {
                          "type": "array",
                          "items": {
                            "type": "object",
                            "properties": {
                              "piatto_scelto": { "type": "string" },
                              "tipologia_piatto": { 
                                "type": "string",
                                "enum": ["Primo", "Secondo", "Piatto unico", "Insalata", "Dolce"]
                              },
                              "motivazione": { "type": "string" },
                              "livello_equilibrio": { "type": "integer", "minimum": 1, "maximum": 10 }
                            },
                            "required": ["piatto_scelto","tipologia_piatto","motivazione","livello_equilibrio"],
                            "additionalProperties": false
                          }
                        }
                      },
                      "required": ["scelte"],
                      "additionalProperties": false
                    }
                    """u8.ToArray()),
                    jsonSchemaIsStrict: true
                ),
                        Temperature = 0.2f
            };

            ChatCompletion completion = await _chatClient.CompleteChatAsync(
                new ChatMessage[]
                {
            new SystemChatMessage("Sei un nutrizionista esperto."),
            new UserChatMessage(prompt)
                },
                options
            );

            try
            {
                using JsonDocument structuredJson = JsonDocument.Parse(completion.Content[0].Text);
                var scelteJson = structuredJson.RootElement.GetProperty("scelte");
                var list = new List<MealRecommendationDTO>();

                foreach (var item in scelteJson.EnumerateArray())
                {
                    list.Add(new MealRecommendationDTO
                    {
                        Piatto_scelto = item.GetProperty("piatto_scelto").GetString()!,
                        Tipologia_piatto = item.GetProperty("tipologia_piatto").GetString()!,
                        Motivazione = item.GetProperty("motivazione").GetString()!,
                        Livello_equilibrio = item.GetProperty("livello_equilibrio").GetInt32()
                    });
                }

                return list;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore deserializzazione AI: {ex.Message}");
                return null;
            }
        }

        private string BuildNutritionPrompt(string menuText, string[] forbiddenIngredients, MealHistory[] last5Meals, bool aggiungiDolce)
        {
            // Ordina per data crescente (vecchio → nuovo)
            var last5Ordered = last5Meals
                .OrderBy(m => DateTime.Parse(m.Date))
                .TakeLast(5)
                .GroupBy(m => m.Date)
                .ToArray();

            var sb = new StringBuilder();

            sb.AppendLine("Sei un nutrizionista razionale che deve scegliere uno o due piatti dal menu.");
            sb.AppendLine();
            sb.AppendLine("Nota importante: Genera sempre un solo piatto principale (Primo, Secondo, Piatto unico o Insalata).");
            sb.AppendLine($"Aggiungi un dolce come extra opzionale SOLO se {aggiungiDolce} == True. Mai come unico piatto e non ogni giorno.");
            sb.AppendLine();
            sb.AppendLine("OBIETTIVO PRINCIPALE:");
            sb.AppendLine("1. Scegliere sempre almeno un piatto principale.");
            sb.AppendLine("2. Massimizzare la varietà settimanale dei piatti scelti evitando ripetizioni e mantenendo equilibrio.");
            sb.AppendLine();
            sb.AppendLine("MENU DI OGGI:");
            sb.AppendLine(menuText);
            sb.AppendLine();
            sb.AppendLine("INGREDIENTI DA EVITARE:");
            if (forbiddenIngredients.Length > 0)
            {
                foreach (var ing in forbiddenIngredients)
                    sb.AppendLine($"- {ing}");
            }
            else
            {
                sb.AppendLine("- nessuno");
            }

            sb.AppendLine();
            sb.AppendLine("STORICO ULTIMI 5 GIORNI:");
            foreach (var group in last5Ordered)
            {
                DateTime date = DateTime.Parse(group.Key);
                string dayLabel = date.ToString("ddd", new System.Globalization.CultureInfo("it-IT")); // Lun, Mar, Mer...
                var dishes = string.Join(", ", group.Select(m => $"{m.DishName} ({m.TipologiaPiatto})"));
                sb.AppendLine($"{dayLabel}: {dishes}");
            }

            sb.AppendLine();
            sb.AppendLine("REGOLE OBBLIGATORIE:");
            sb.AppendLine("1. Non scegliere lo stesso identico piatto del giorno precedente.");
            sb.AppendLine("2. Massimizzare la varietà della tipologia di piatto tra: Primo, Secondo, Piatto unico, Insalata.");
            sb.AppendLine("3. Penalizzare la ripetizione della stessa tipologia di piatto per più giorni consecutivi.");
            sb.AppendLine($"4. Dolci possono essere aggiunti solo come extra opzionale se {aggiungiDolce} == True, mai come unico piatto.");
            sb.AppendLine("5. Non inventare piatti non presenti nel menu.");
            sb.AppendLine("6. Preferire piatti non fritti e mantenere equilibrio generale.");

            sb.AppendLine();
            sb.AppendLine("CRITERIO DECISIONALE:");
            sb.AppendLine("1. Analizza gli ultimi 5 giorni.");
            sb.AppendLine("2. Conta quante volte è stata scelta ogni tipologia di piatto.");
            sb.AppendLine("3. Individua la tipologia meno rappresentata.");
            sb.AppendLine("4. Scegli sempre almeno un piatto principale.");
            sb.AppendLine("5. Se più opzioni rispettano le regole, scegli quella che aumenta maggiormente la diversità complessiva.");

            sb.AppendLine();
            sb.AppendLine("Rispondi SOLO con JSON valido, senza testo extra.");
            sb.AppendLine("Formato:");
            sb.AppendLine("{");
            sb.AppendLine("  \"scelte\": [");
            sb.AppendLine("    {");
            sb.AppendLine("      \"piatto_scelto\": \"nome piatto principale\",");
            sb.AppendLine("      \"tipologia_piatto\": \"Primo | Secondo | Piatto unico | Insalata\",");
            sb.AppendLine("      \"motivazione\": \"spiegazione breve e concreta\",");
            sb.AppendLine("      \"livello_equilibrio\": numero da 1 a 10");
            sb.AppendLine("    }");

            if (aggiungiDolce)
            {
                sb.AppendLine("    ,{");
                sb.AppendLine("      \"piatto_scelto\": \"nome dolce opzionale\",");
                sb.AppendLine("      \"tipologia_piatto\": \"Dolce\",");
                sb.AppendLine("      \"motivazione\": \"scelta opzionale per variare la settimana\",");
                sb.AppendLine("      \"livello_equilibrio\": numero da 1 a 10");
                sb.AppendLine("    }");
            }

            sb.AppendLine("  ]");
            sb.AppendLine("}");

            return sb.ToString();
        }

    }
}
