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

        public override bool Equals(object obj)
        {
            //Check for null and compare run-time types.
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            else
            {
                Punto p = (Punto)obj;
                return (Ordinata == p.Ordinata) && (Ascissa == p.Ascissa);
            }
        }

        // Operators
        public static bool operator ==(Punto  a, Punto b)
        {
            return Equals(a, b);
        }

        public static bool operator != (Punto a, Punto b)
        {
            return !Equals(a, b);
        }

        public override string ToString()
        {
            return "Ascissa (X): " + Ascissa + " - " + "Ordinata (Y): " + Ordinata;
        }
    }
}
