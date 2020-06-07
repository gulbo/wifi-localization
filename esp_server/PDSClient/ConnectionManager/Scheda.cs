using System.Text;

namespace PDSClient.ConnectionManager
{
    public class Scheda
    {

        //Proprietà

        public int ID_scheda {get; set;}
        public Punto Punto {get; set;}

        public Scheda()
        {

        }

        public Scheda(int id_scheda, Punto punto)
        {
            this.ID_scheda = id_scheda;
            this.Punto = punto;
        }

        public Scheda(int id_scheda, double x, double y)
        {
            this.ID_scheda = id_scheda;
            this.Punto = new Punto(x,y);
        }

        public bool Equals(Scheda scheda)
        {
            return this.ID_scheda == scheda.ID_scheda;
        }

        public override string ToString()
        {
            StringBuilder builderStringa = new StringBuilder();
            builderStringa.Append("ID scheda: ").Append(this.ID_scheda).Append(" - ");
            builderStringa.Append("Ascissa (X): ").Append(this.Punto.Ascissa).Append(" - ");
            builderStringa.Append("Ordinata (Y): ").Append(this.Punto.Ordinata);
            return builderStringa.ToString();
        }
    }
}
