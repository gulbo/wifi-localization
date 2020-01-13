using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDSClient.ConnectionManager
{
    public class Board
    {

        public int Id { get; set; }
        public Point P { get; set; }

        public Board()
        {

        }

        public Board(int id, double x, double y)
        {
            this.Id = id;
            this.P = new Point(x,y);
        }

        public Board(int id, Point p)
        {
            this.Id = id;
            this.P = p;
        }

        public bool Equals(Board board)
        {
            return this.Id == board.Id;
        }

        public override string ToString()
        {
            return "BoardId: " + Id + " - " + "X: " + P.X + " - " + "Y: " + P.Y;
        }
    }
}
