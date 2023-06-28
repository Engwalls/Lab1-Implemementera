using System;
using System.Collections.Generic;

// Singleton, Strategy, and Observer patterns

// Define an interface for ship placement
// Strategy pattern
public interface IShipPlacementStrategy
{
    void PlaceShips(Board board, Ship[] ships);
}

// Implementation of the IShipPlacementStrategy interface that randomly places ships
public class RandomShipPlacementStrategy : IShipPlacementStrategy
{
    private static readonly Random _random = new Random();

    public void PlaceShips(Board board, Ship[] ships)
    {
        // Place each ship on the board
        foreach (var ship in ships)
        {
            bool isPlaced = false;

            // Keep trying to place the ship until it is placed
            while (!isPlaced)
            {
                // Generate random coordinates and orientation for the ship
                int row = _random.Next(0, board.Rows);
                int column = _random.Next(0, board.Columns);
                bool isVertical = _random.Next(0, 2) == 0;

                // Check if the ship can be placed at these coordinates
                if (board.CanPlaceShip(ship, row, column, isVertical))
                {
                    isPlaced = true;

                    // Place the ship on the board
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
        // Welcome message
        Console.WriteLine("Welcome to World War II, it's time to bomb some boats!");
        Console.WriteLine("------------------------------------------------------");
        Console.WriteLine("Press any button to start the game!");
        Console.ReadKey();

        // Create a board with random ship placement
        var board = Board.GetInstance(7, 7, new RandomShipPlacementStrategy());
        board.Initialize();

        // Create an observer for ship sink notifications and add it to the board
        var shipSunkObserver = new ShipSunkObserver();
        board.AddObserver(shipSunkObserver);

        // Place the ships on the board
        board.PlaceShips();

        // Game loop until the game is over
        while (!board.IsGameOver())
        {
            // Clear the console and print the board
            Console.Clear();
            board.Print();

            // Read user input for entering coordinates to attack
            Console.WriteLine("Enter coordinates (e.g., A1): ");
            string input = Console.ReadLine().ToUpper();

            bool isValidInput = board.IsValidInput(input);
            bool isHit = false;

            // If the input is valid, perform the attack
            if (isValidInput)
            {
                isHit = board.Attack(input);
            }

            // Print the result of the attack
            Console.WriteLine(isValidInput ? (isHit ? "Hit!" : "Miss!") : "Invalid input!");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        // The game is over, print the final result
        Console.Clear();
        board.Print();
        Console.WriteLine("Game Over!");
    }
}

// The class that represents a ship
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

// The class that represents a cell on the board
public class Cell
{
    public bool IsHit { get; set; }
    public Ship? Ship { get; set; }
}

// Interface for an observer
// Observer pattern
public interface IObserver
{
    void Notify(string message);
}

// Observer that writes messages when a ship is sunk
public class ShipSunkObserver : IObserver
{
    public void Notify(string message)
    {
        Console.WriteLine(message);
    }
}

// The class that represents the board
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

    // Private constructor used by the Singleton pattern to create the board
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

    // Method to get an instance of the board using the Singleton pattern
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

    // Adds an observer to the board
    public void AddObserver(IObserver observer)
    {
        _observers.Add(observer);
    }

    // Initializes the board by creating cells
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

    // Places the ships on the board using the selected strategy
    public void PlaceShips()
    {
        _shipPlacementStrategy.PlaceShips(this, _ships);
    }

    // Checks if a ship can be placed at the given coordinates and orientation
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

    // Checks if the user's attack input is valid
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

    // Performs an attack based on the user's input and returns whether the attack was a hit
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

    // Checks if the game is over
    public bool IsGameOver()
    {
        return _shipsSunk == _ships.Length;
    }

    // Notifies all observers with a message
    public void NotifyObservers(string message)
    {
        foreach (var observer in _observers)
        {
            observer.Notify(message);
        }
    }

    // Prints the board to the console
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
