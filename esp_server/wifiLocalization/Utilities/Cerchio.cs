using System;
using System.Linq;
using System.Collections.Generic;

namespace WifiLocalization.Utilities
{
    public class Cerchio
    {
        public Punto Centro { get; set; }
        public double Raggio { get; set; }
        
        static double Converti_RSSI(int rssi)
        {
            const double costante_propagazione = 3.3; 
            const double potenza_segnale = -60; //dBm
            
            // Calcolo la distanza tra due nodi conoscendo il valore di RSSI ed i parametri sopra definiti 
            //      distanza = 10^( (potenza_segnale-rssi) / (10 * costante_propagazione))
            
            return Math.Pow(10, (potenza_segnale - rssi) / (10 * costante_propagazione));
        }

        public Cerchio(Punto centro, int rssi)
        {
            Centro = centro;
            Raggio = Converti_RSSI(rssi);
        }

        public bool Ctrl_Intersezione(Cerchio cerchio)
        {
            // Calcolo la distanza tra i due centri e la somma dei due raggi
            double distanza_x = Centro.Ascissa - cerchio.Centro.Ascissa;
            double distanza_y = Centro.Ordinata - cerchio.Centro.Ordinata;
            double distanza_centri = Math.Sqrt((distanza_x * distanza_x) + (distanza_y * distanza_y));
            double somma_raggi = (Raggio + cerchio.Raggio);
            
            // Verifico l'intersezione
            if (distanza_centri <= somma_raggi)
                return true; // Intersecano
            else
                return false; // Non intersecano
        }

        public Punto Interseca(Cerchio cerchio)
        {
            /*  
                - i punti A e B sono i centri dei due cerchi
                - i punti C e D sono i punti di intersezione dei due cerchi
                - P è il centro dell'intersezione tra i due cerchi = punto medio del segmento cd 
                - il raggio del cerchio A è la lunghezza dei segmenti ac e ad (noto)
                - il raggio del cerchio B è la lunghezza dei segmenti bc e bd (noto)
                - la distanza tra i due centri è la lunghezza del segmento ab
            */

            // Calcolo la lunghezza del segmento ab  = la distanza tra i due centri
            double distanza_x = Centro.Ascissa - cerchio.Centro.Ascissa;
            double distanza_y = Centro.Ordinata - cerchio.Centro.Ordinata;
            double distanza_centri = Math.Sqrt((distanza_x * distanza_x) + (distanza_y * distanza_y));

            // Calcolo la lunghezza del segmento che unisce il centro del cerchio A con il punto P (segmento ap):
            double ap = (((Raggio * Raggio) - (cerchio.Raggio * cerchio.Raggio) + (distanza_centri * distanza_centri)) / (2 * distanza_centri));
            
            // Conoscendo sia ap che ac utilizzo il teorema di pitagora per calcolarmi il segmento cp 
            double cp = Math.Sqrt((Raggio * Raggio) - (ap * ap));

            // Conoscendo sia ap che ab calcolo le coordinate del punto P (centro dell'intersezione tra i due cerchi)
            double p_ascissa = Centro.Ascissa + ap * (cerchio.Centro.Ascissa - Centro.Ascissa) / distanza_centri;
            double p_ordinata = Centro.Ordinata + ap * (cerchio.Centro.Ordinata - Centro.Ordinata) / distanza_centri;
            
            // Ritorno il centro dell'intersezione tra i due cerchi
            Punto intersezione;
            intersezione = new Punto(p_ascissa, p_ordinata);
            return intersezione;
        }

        private Punto Media_Pesata(Cerchio cerchio)
        {
            /*  
               - i punti A e B sono i centri dei due cerchi
               - la distanza tra i due centri è la lunghezza del segmento ab
               - i punti C e D sono i punti in cui i due cerchi intersecano il segmento ab
               - il segmento cd è il segmento su cui devo trovare un nuovo punto P (in "proporzione" ai raggi dei due cerchi)
           */

            double somma_raggi = Raggio + cerchio.Raggio;
            double peso_1, peso_2;
            double ascissa, ordinata;

            // Calcolo i pesi e controllo che siano validi
            peso_1 = Raggio / somma_raggi;
            System.Diagnostics.Debug.Assert(peso_1 >= 0 && peso_1 <= 1);
            peso_2 = cerchio.Raggio / somma_raggi;
            System.Diagnostics.Debug.Assert(peso_2 >= 0 && peso_2 <= 1);

            // Calcolo le coordinate del nuovo punto P
            ascissa = this.Centro.Ascissa * peso_2 + cerchio.Centro.Ascissa * peso_1;
            ordinata = this.Centro.Ordinata * peso_2 + cerchio.Centro.Ordinata * peso_1;

            // Ritorno un punto sul segmento cd 
            return new Punto(ascissa, ordinata);
        }

        public static Punto Intersezione(ICollection<Cerchio> raccolta)
        {
            int totale_punti = 0;
            Punto intersezione = new Punto(0, 0);
            List<Punto> lista_punti = new List<Punto>();
    
            for (int i = 0; i < raccolta.Count - 1; i++)
            {
                for (int j = i + 1; j < raccolta.Count; j++)
                {
                    if (raccolta.ElementAt(i).Ctrl_Intersezione(raccolta.ElementAt(j)))
                    {
                        lista_punti.Add(raccolta.ElementAt(i).Interseca(raccolta.ElementAt(j)));
                    }
                    else
                    {
                        lista_punti.Add(raccolta.ElementAt(i).Media_Pesata(raccolta.ElementAt(j)));
                    }
                }
            }

            foreach (Punto punto in lista_punti)
            {
                intersezione.Ascissa += punto.Ascissa;
                intersezione.Ordinata += punto.Ordinata;
                totale_punti++;
            }

            intersezione.Ascissa /= totale_punti;
            intersezione.Ordinata /= totale_punti;
            return intersezione;
        }    
    }
}
