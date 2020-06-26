using System.Text;

namespace PDSClient.ConnectionManager
{
    public class Scheda
    {

        //Proprietà

        public int ID_scheda { get; set; }
        public Punto Punto { get; set; }

        public Scheda()
        {
            Punto = new Punto(0, 0);
        }

        public Scheda(int id_scheda, Punto punto)
        {
            ID_scheda = id_scheda;
            Punto = punto;
        }

        public Scheda(int id_scheda, double x, double y)
        {
            ID_scheda = id_scheda;
            Punto = new Punto(x,y);
        }

        public bool Equals(Scheda scheda)
        {
            return ID_scheda == scheda.ID_scheda;
        }

        public override bool Equals(object obj)
        {
            //Check for null and compare run-time types.
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            else
            {
                Scheda board = (Scheda)obj;
                if (ID_scheda == board.ID_scheda)
                {
                    return Punto.Equals(board.Punto);
                }
            }
            return false;
        }

        public override string ToString()
        {
            StringBuilder builderStringa = new StringBuilder();
            builderStringa.Append("ID scheda: ").Append(ID_scheda).Append(" - ");
            builderStringa.Append("Ascissa (X): ").Append(Punto.Ascissa).Append(" - ");
            builderStringa.Append("Ordinata (Y): ").Append(Punto.Ordinata);
            return builderStringa.ToString();
        }
    }
}
