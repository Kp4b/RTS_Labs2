using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Timers;
using System.Diagnostics;

internal class Program
{
    private static TimeMeasurement timer = new TimeMeasurement();
    private static Semaphore readSemaphore = new Semaphore(0, 3);
    private static Semaphore avgSemaphore = new Semaphore(0, 3);
    private static Semaphore mainSemaphore = new Semaphore(0, 1);
    private static double[] A = [], B = [], C = [], R = [];
    private static int dataSize;
    private static double avgA = 0, avgB = 0, avgC = 0;
    private static Stopwatch semaphoreStopwatch = new Stopwatch();

    public static void Main(string[] args)
    {
        // Request the size of the data files from the user
        Console.Write("Enter the size of the data files in KB: ");
        if (!int.TryParse(Console.ReadLine(), out int dataSizeInKB))
        {
            Console.WriteLine("Invalid input. Please enter a valid number.");
            return;
        }

        // Convert size from KB to number of double values
        dataSize = dataSizeInKB * 1024 / sizeof(double);

        // Generate random data and write to files
        GenerateRandomDataFile("A.txt", dataSize);
        GenerateRandomDataFile("B.txt", dataSize);
        GenerateRandomDataFile("C.txt", dataSize);

        Console.WriteLine("Files A.txt, B.txt, and C.txt have been created with random data.");

        // Create threads for reading data
        Thread readAThread = new Thread(() =>
        {
            A = ReadArrayFromFile("A.txt");
            readSemaphore.Release();
        });

        Thread readBThread = new Thread(() =>
        {
            B = ReadArrayFromFile("B.txt");
            readSemaphore.Release();
        });

        Thread readCThread = new Thread(() =>
        {
            C = ReadArrayFromFile("C.txt");
            readSemaphore.Release();
        });

        // Create threads for calculating averages
        Thread avgAThread = new Thread(() =>
        {
            avgA = CalculateAverage(A);
            avgSemaphore.Release();
        });

        Thread avgBThread = new Thread(() =>
        {
            avgB = CalculateAverage(B);
            avgSemaphore.Release();
        });

        Thread avgCThread = new Thread(() =>
        {
            avgC = CalculateAverage(C);
            avgSemaphore.Release();
        });

        // Create threads for main tasks
        Thread readDataThread = new Thread(ReadData);
        Thread calculateResultThread = new Thread(CalculateResult);
        Thread writeResultThread = new Thread(WriteResult);

        // Start threads
        readAThread.Start();
        readBThread.Start();
        readCThread.Start();
        avgAThread.Start();
        avgBThread.Start();
        avgCThread.Start();
        readDataThread.Start();
        calculateResultThread.Start();
        writeResultThread.Start();

        // Start measuring time
        timer.Start();

        semaphoreStopwatch.Start();

        // Wait for all threads to complete
        mainSemaphore.WaitOne();
        Console.WriteLine("All tasks completed.");

        // Display semaphore time
        Console.WriteLine($"Semaphore Time: {semaphoreStopwatch.Elapsed.TotalSeconds} seconds");
    }

    private static void ReadData()
    {
        semaphoreStopwatch.Start();
        // Wait for files data reading to complete
        for (int i = 0; i < 3; i++)
        {
            readSemaphore.WaitOne();
        }

        Console.WriteLine("Data has been read from files.");

        // Signal that data reading is complete
        mainSemaphore.Release();
        semaphoreStopwatch.Stop();
    }

    private static void CalculateResult()
    {
        semaphoreStopwatch.Start();
        // Wait for data reading to complete
        mainSemaphore.WaitOne();

        // Wait for averages to be calculated
        for (int i = 0; i < 3; i++)
        {
            avgSemaphore.WaitOne();
        }

        R = CalculateResultArray(A, B, C, avgA, avgB, avgC);

        Console.WriteLine("Result array has been calculated.");

        // Signal that calculation is complete
        mainSemaphore.Release();
        semaphoreStopwatch.Stop();
    }

    private static void WriteResult()
    {
        semaphoreStopwatch.Start();
        // Wait for calculation to complete
        mainSemaphore.WaitOne();

        WriteArrayToFile("R.txt", R);

        Console.WriteLine("Result array has been written to file.");

        // Stop measuring time
        timer.Stop();

        // Get elapsed time and process time
        double elapsedTime = timer.GetElapsedTime();
        double elapsedTimeUsingTimeGetTime = timer.GetElapsedTimeUsingTimeGetTime();
        TimeSpan processTime = timer.GetProcessTime();

        // Display elapsed time and process time
        Console.WriteLine($"Elapsed Time (QueryPerformanceCounter): {elapsedTime} seconds");
        Console.WriteLine($"Elapsed Time (timeGetTime): {elapsedTimeUsingTimeGetTime} seconds");
        Console.WriteLine($"Process Time: {processTime.TotalSeconds} seconds");

        // Signal that writing is complete
        mainSemaphore.Release();
        semaphoreStopwatch.Stop();
    }

    private static void GenerateRandomDataFile(string fileName, int dataSize)
    {
        Random random = new Random();
        using (StreamWriter writer = new StreamWriter(fileName, false)) // Open file with overwrite mode
        {
            for (int i = 0; i < dataSize; i++)
            {
                double randomValue = random.NextDouble() * 100; // Generate a random double value between 0 and 100
                writer.WriteLine(randomValue);
            }
        }
    }

    private static double[] ReadArrayFromFile(string fileName)
    {
        string[] lines = File.ReadAllLines(fileName);
        double[] array = new double[lines.Length];
        for (int i = 0; i < lines.Length; i++)
        {
            array[i] = double.Parse(lines[i]);
        }
        return array;
    }

    private static void WriteArrayToFile(string fileName, double[] array)
    {
        using (StreamWriter writer = new StreamWriter(fileName))
        {
            foreach (double value in array)
            {
                writer.WriteLine(value);
            }
        }
    }

    private static double[] CalculateResultArray(double[] A, double[] B, double[] C, double avgA, double avgB, double avgC)
    {
        int length = A.Length;
        double[] R = new double[length];

        for (int i = 0; i < length; i++)
        {
            R[i] = A[i] * avgA + B[i] * avgB + C[i] * avgC;
        }

        return R;
    }

    private static double CalculateAverage(double[] array)
    {
        double sum = 0;
        foreach (double value in array)
        {
            sum += value;
        }
        return sum / array.Length;
    }
}
