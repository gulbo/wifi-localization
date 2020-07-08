namespace WifiLocalization.ConnectionManager
{
    public class Punto
    {

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

        public static bool operator == (Punto  a, Punto b)
        {
            return Equals(a, b);
        }

        public static bool operator != (Punto a, Punto b)
        {
            return !Equals(a, b);
        }

        public override bool Equals(object oggetto)
        {
            if ((oggetto == null) || !this.GetType().Equals(oggetto.GetType()))
            {
                return false;
            }
            else
            {
                Punto punto = (Punto)oggetto;
                return (Ordinata == punto.Ordinata) && (Ascissa == punto.Ascissa);
            }
        }

        public override string ToString()
        {
            return "Ascissa (X): " + Ascissa + " - " + "Ordinata (Y): " + Ordinata;
        }
    }
}
