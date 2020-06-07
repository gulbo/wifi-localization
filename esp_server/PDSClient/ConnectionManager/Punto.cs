namespace PDSClient.ConnectionManager
{
    public class Punto
    {

        //Proprietà

        public double Ascissa { get; set; }
        public double Ordinata { get; set; }

        public Punto()
        {

        }

        public Punto(double x, double y)
        {
            Ascissa = x;
            Ordinata = y;
        }

        public override string ToString()
        {
            return "Ascissa (X): " + Ascissa + " - " + "Ordinata (Y): " + Ordinata;
        }
    }
}
