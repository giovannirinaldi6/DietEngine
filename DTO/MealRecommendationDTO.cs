using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DietWorker.DTO
{
    public class MealRecommendationDTO
    {
        public string Piatto_scelto { get; set; } = "";

        // Rimuovi Categoria_proteina se non ti serve più
        // oppure mantienila se vuoi ancora tracciare il tipo di proteina
        public string Categoria_proteina { get; set; } = "";

        // Nuovo campo obbligatorio per distinguere tipologia
        public string Tipologia_piatto { get; set; } = ""; // Primo | Secondo | Piatto unico | Insalata | Dolce

        public string Motivazione { get; set; } = "";

        // Rinomina Livello_salute -> Livello_equilibrio se vuoi coerenza col nuovo prompt
        public int Livello_equilibrio { get; set; }
    }
}