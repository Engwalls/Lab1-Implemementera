using System;
using System.Collections.Generic;

// Singleton, Strategy och Observermönster

// Definiera ett gränssnitt för att placera skepp
// Strategy-mönster
public interface IShipPlacementStrategy
{
    void PlaceShips(Board board, Ship[] ships);
}

// Implementation av gränssnittet IShipPlacementStrategy som slumpmässigt placerar skepp
public class RandomShipPlacementStrategy : IShipPlacementStrategy
{
    private static readonly Random _random = new Random();

    public void PlaceShips(Board board, Ship[] ships)
    {
        // Placera varje skepp på brädet
        foreach (var ship in ships)
        {
            bool isPlaced = false;

            // Fortsätt försöka placera skeppet tills det är placerat
            while (!isPlaced)
            {
                // Generera slumpmässiga koordinater och orientering för skeppet
                int row = _random.Next(0, board.Rows);
                int column = _random.Next(0, board.Columns);
                bool isVertical = _random.Next(0, 2) == 0;

                // Kontrollera om skeppet kan placeras på dessa koordinater
                if (board.CanPlaceShip(ship, row, column, isVertical))
                {
                    isPlaced = true;

                    // Placera skeppet på brädet
                    for (int i = 0; i < ship.Length; i++)
                    {
                        if (isVertical)
                        {
                            board.Cells[row + i, column].Ship = ship;
                        }
                        else
                        {
                            board.Cells[row, column + i].Ship = ship;
                        }
                    }
                }
            }
        }
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        // Välkomstmeddelande
        Console.WriteLine("Welcome to World War II, its time to bombs some boats!");
        Console.WriteLine("------------------------------------------------------");
        Console.WriteLine("Press any button to start the game!");
        Console.ReadKey();

        // Skapa ett bräde med slumpmässig skeppplacering
        var board = Board.GetInstance(7, 7, new RandomShipPlacementStrategy());
        board.Initialize();

        // Skapa en observatör för skeppssänkningar och lägg till den i brädet
        var shipSunkObserver = new ShipSunkObserver();
        board.AddObserver(shipSunkObserver);

        // Placera skeppen på brädet
        board.PlaceShips();

        // Spelloop tills spelet är över
        while (!board.IsGameOver())
        {
            // Rensa konsolen och skriv ut brädet
            Console.Clear();
            board.Print();

            // Läs in användarens input för att ange koordinater att attackera
            Console.WriteLine("Enter coordinates (for example, A1): ");
            string input = Console.ReadLine().ToUpper();

            bool isValidInput = board.IsValidInput(input);
            bool isHit = false;

            // Om input är giltig, utför attacken
            if (isValidInput)
            {
                isHit = board.Attack(input);
            }

            // Skriv ut resultatet av attacken
            Console.WriteLine(isValidInput ? (isHit ? "Hit!" : "Miss!") : "Invalid input!");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        // Spelet är över, skriv ut slutresultatet
        Console.Clear();
        board.Print();
        Console.WriteLine("Game Over!");
    }
}

// Klassen som representerar ett skepp
public class Ship
{
    public string Name { get; }
    public int Length { get; set; }
    public bool IsSunk => Length == 0;

    public Ship(string name, int length)
    {
        Name = name;
        Length = length;
    }
}

// Klassen som representerar en cell på brädet
public class Cell
{
    public bool IsHit { get; set; }
    public Ship? Ship { get; set; }
}

// Gränssnitt för en observatör
// Observer-mönster
public interface IObserver
{
    void Notify(string message);
}

// Observatör som skriver ut meddelanden när skepp sänks
public class ShipSunkObserver : IObserver
{
    public void Notify(string message)
    {
        Console.WriteLine(message);
    }
}

// Klassen som representerar brädet
public class Board
{
    private readonly int _rows;
    private readonly int _columns;
    private readonly Cell[,] _cells;
    private readonly Ship[] _ships;
    private int _shipsSunk;

    private readonly List<IObserver> _observers;
    private static Board _instance;
    private static readonly object _lock = new object();
    private IShipPlacementStrategy _shipPlacementStrategy;

    public int Rows => _rows;
    public int Columns => _columns;
    public Cell[,] Cells => _cells;

    // Privat konstruktor som används av Singleton-mönstret för att skapa brädet
    private Board(int rows, int columns, IShipPlacementStrategy shipPlacementStrategy)
    {
        _rows = rows;
        _columns = columns;
        _cells = new Cell[rows, columns];
        _ships = new Ship[]
        {
            new Ship("Patrol Boat", 6),
            new Ship("Aircraft Carrier", 5),
            new Ship("Battleship", 4),
            new Ship("Cruiser", 3),
            new Ship("Submarine", 3),
            new Ship("Destroyer", 2)
        };

        _observers = new List<IObserver>();
        _shipPlacementStrategy = shipPlacementStrategy;
    }

    // Metod för att få en instans av brädet genom Singleton-mönstret
    // Singleton-mönster

    public static Board GetInstance(int rows, int columns, IShipPlacementStrategy shipPlacementStrategy)
    {
        if (_instance == null)
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = new Board(rows, columns, shipPlacementStrategy);
                }
            }
        }
        return _instance;
    }

    // Lägger till en observatör till brädet
    public void AddObserver(IObserver observer)
    {
        _observers.Add(observer);
    }

    // Initierar brädet genom att skapa celler
    public void Initialize()
    {
        for (int row = 0; row < _rows; row++)
        {
            for (int column = 0; column < _columns; column++)
            {
                _cells[row, column] = new Cell();
            }
        }
    }

    // Placerar skeppen på brädet med hjälp av den valda strategin
    public void PlaceShips()
    {
        _shipPlacementStrategy.PlaceShips(this, _ships);
    }

    // Kontrollerar om ett skepp kan placeras på givna koordinater och orientering
    public bool CanPlaceShip(Ship ship, int row, int column, bool isVertical)
    {
        if (isVertical && row + ship.Length > _rows)
        {
            return false;
        }

        if (!isVertical && column + ship.Length > _columns)
        {
            return false;
        }

        for (int i = 0; i < ship.Length; i++)
        {
            if (isVertical && _cells[row + i, column].Ship != null)
            {
                return false;
            }

            if (!isVertical && _cells[row, column + i].Ship != null)
            {
                return false;
            }
        }

        return true;
    }

    // Kontrollerar om användarens input för attack är giltig
    public bool IsValidInput(string input)
    {
        if (input.Length < 2 || input.Length > 3)
        {
            return false;
        }

        char columnChar = input[0];
        int row;
        bool isValidRow = int.TryParse(input.Substring(1), out row);

        if (!isValidRow)
        {
            return false;
        }

        int column = columnChar - 'A';

        if (row < 1 || row > _rows || column < 0 || column >= _columns)
        {
            return false;
        }

        return true;
    }

    // Utför en attack baserat på användarens input och returnerar om attacken var en träff
    public bool Attack(string input)
    {
        char columnChar = input[0];
        int row = int.Parse(input.Substring(1)) - 1;
        int column = columnChar - 'A';

        Cell cell = _cells[row, column];

        if (cell.IsHit)
        {
            return false;
        }

        cell.IsHit = true;

        if (cell.Ship != null)
        {
            cell.Ship.Length--;

            if (cell.Ship.IsSunk)
            {
                _shipsSunk++;
                NotifyObservers($"You sank the {cell.Ship.Name}!");

                if (_shipsSunk == _ships.Length)
                {
                    return true;
                }
            }

            return true;
        }

        return false;
    }

    // Kontrollerar om spelet är över
    public bool IsGameOver()
    {
        return _shipsSunk == _ships.Length;
    }

    // Meddelar alla observatörer med ett meddelande
    public void NotifyObservers(string message)
    {
        foreach (var observer in _observers)
        {
            observer.Notify(message);
        }
    }

    // Skriver ut brädet till konsolen
    public void Print()
    {
        Console.WriteLine("  A B C D E F G");

        for (int row = 0; row < _rows; row++)
        {
            Console.Write($"{row + 1} ");

            for (int column = 0; column < _columns; column++)
            {
                Cell cell = _cells[row, column];

                if (cell.IsHit && cell.Ship != null)
                {
                    Console.Write("X ");
                }
                else if (cell.IsHit)
                {
                    Console.Write("O ");
                }
                else
                {
                    Console.Write("- ");
                }
            }

            Console.WriteLine();
        }
    }
}
