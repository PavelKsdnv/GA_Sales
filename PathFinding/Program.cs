using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace PathFinding
{
    class City
    {
        public int id;
        public List<City> neighbours;
        public int posX;
        public int posY;
    }
    class Program
    {
        static City[] CreateCities(
            int numCities,
            int gridSize)
        {
            var r = new Random();
            var town = new City[numCities];
            for (int i = 0; i < numCities; i++)
            {
                town[i] = new City
                {
                    id = i,
                    neighbours = new List<City>(),
                    posX = r.Next(gridSize),
                    posY = r.Next(gridSize)
                };
            }

            for (int i = 0; i < numCities; i++)
                for (int j = 0; j < numCities; j++)
                    if (i != j)
                        town[i].neighbours.Add(town[j]);

            return town;
        }

        static void DrawTown(
            City[] town,
            char[, ] matrix, 
            int[] order)
        {
            for (int i = 0; i < town.Length; i++)
            {
                matrix[town[i].posX, town[i].posY] = Convert.ToChar(town[i].id + 65);
            }

            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                for (int j = 0; j < matrix.GetLength(1); j++)
                {
                    if ((matrix[i, j]) == '#')
                        Console.ForegroundColor = ConsoleColor.Red;
                    else if (matrix[i, j] != '-')
                        Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write(matrix[i, j] + " ");
                    Console.ResetColor();
                }
                Console.WriteLine();
            }

            Console.Write(Convert.ToChar(order[0] + 65));
            for (int i = 1; i < order.Length; i++)
            {
                Console.Write($" --> {Convert.ToChar(order[i] + 65)}");
            }
        }

        static int[,] CalculateDistances(City[] town)
        {
            // Връща матрица от разстояния, между всички градове
            int cityNum = town.Length;
            int[,] dist = new int[cityNum, cityNum];
            for (int i = 0; i < cityNum; i++)
            {
                for (int j = 0; j < cityNum; j++)
                {
                    if (i == j)
                    {
                        dist[i, j] = -1;
                        continue;
                    }

                    if (town[i].neighbours.Contains(town[j]))
                        //Изчислявам разстояните между два града, като го умножавам по 10, за да използвам цели числа
                        dist[i, j] = Convert.ToInt32(10 * Math.Sqrt((town[i].posX - town[j].posX) * (town[i].posX - town[j].posX) +
                                     (town[i].posY - town[j].posY) * (town[i].posY - town[j].posY)));
                }
            }
            return dist;
        }

        static int[] GenerateSeed(
            City[] town,
            Random r)
        {
            // Създава случаен маршрут
            int citiesNum = town.Length;
            var order = new int[town.Length + 1];
            order[0] = r.Next(citiesNum);
            var visited = new List<int>();
            visited.Add(order[0]);
            for (int i = 1; i < citiesNum; i++)
            {
                // Избира случаен съсед на предходния град
                var randomNeighbour = town[order[i - 1]].neighbours[r.Next(town[order[i - 1]].neighbours.Count())].id;
                if (visited.Contains(randomNeighbour))
                {
                    --i;
                    continue;
                }
                order[i] = randomNeighbour;
                visited.Add(randomNeighbour);
            }


            order[citiesNum] = order[0];
            return order;
        }

        static int[] FitnessCalc(
            List<int[]> population,
            int[,] distances)
        {
            // Изчислява разстоянието, което ще бъде изминато през всеки от маршрутите в популацията
            int[] fitness = new int[population.Count()];
            for (int orderId = 0; orderId < population.Count(); orderId++)
            {
                int sum = 0;
                for (int cityId = 0; cityId < population[0].Length - 1; cityId++)
                {
                    sum += distances[population[orderId][cityId], population[orderId][cityId + 1]];
                }
                fitness[orderId] = sum;
            }

            return fitness;
        }

        static int BestPaths(
            int[] fitness,
            out int minFitnessId2)
        {
            // намират се двата най - добри маршрута от дадената популация
            int minFitness = fitness[0];
            int minFitnessId = 0;
            minFitnessId2 = 1;
            for (int i = 1; i < fitness.Length; i++)
            {
                if (fitness[i] < minFitness)
                {
                    minFitness = fitness[i];
                    minFitnessId2 = minFitnessId;
                    minFitnessId = i;
                }
            }
            return minFitnessId;
        }

        static List<int[]> GenerateNextPopulation(
            City[] town,
            int[] parent1,
            int[] parent2,
            int populationSize)
        {
            var r = new Random();
            var population = new List<int[]>();

            // Правим 2/3 от популацията като кръстоска на двата най-добри маршрута
            for (int i = 0; i < populationSize*2 / 3; i++)
            {
                population.Add(new int[parent1.Length]); 
                // Променяме целия маршрут да е -1, защото 0 е валидна стойност (първия град)
                for (int j = 0; j < parent1.Length; j++)
                {
                    population[i][j] = -1;
                }

                // Вземаме случаен регион от маршрута на първия родител
                int startCopy = r.Next(parent1.Length - 1);
                int finishCopy = r.Next(startCopy, parent1.Length - 1);
                for (int j = startCopy; j < finishCopy; j++)
                {
                    population[i][j] = parent1[j];
                }

                // Попълваме останалите места от втория родител
                for (int j = 0; j < parent1.Length - 1; j++)
                {
                    if (population[i][j] == -1)
                    {
                        bool foundFreeCity = false;
                        // Започваме да проверяваме, дали градовете от втория родител са вече сложени и ако са
                        // търсим следващия свободен град
                        for (int k = j; k <= parent1.Length - 1 && !foundFreeCity; k++)
                        {
                            if (k >= parent1.Length - 1)
                                k = 0;
                            if (!ArrayContains(population[i], parent2[k]))
                            {
                                population[i][j] = parent2[k];
                                foundFreeCity = true;
                            }
                        }
                    }
                }
            }
            // Останалата 1/3 я създаваме случайно ,с цел да се избегнат локални екстремуми
            for (int i = population.Count; i < populationSize; i++)
            {
                population.Add(GenerateSeed(town, r));
            }

            // Запазваме най-добрите маршрути от последната популация
            population[populationSize - 1] = parent1;
            population[populationSize - 2] = parent2;
            return population;
        }

        static bool ArrayContains(int[] array, int check)
        {
            for (int i = 0; i < array.Length; i++)
                if (array[i] == check)
                    return true;
            return false;
        }

        static void SwapCities(int[] order, int i, int j)
        {
            int temp = order[i];
            order[i] = order[j];
            order[j] = temp;
        }

        static List<int[]> Mutate(List<int[]> population, int chance)
        {
            var r = new Random();
            int orderLength = population[0].Length;
            // Разменямае два града, при дадена случайност
            for (int i = 0; i < population.Count - 2; i++)
            {
                for (int j = 0; j < orderLength - 1; j++)
                {
                    if (r.Next(100) < chance)
                    {
                        var toBeSwapped = population[i][r.Next(orderLength - 1)];
                        SwapCities(population[i], population[i][j], toBeSwapped);
                    }
                }
                population[i][orderLength - 1] = population[i][0];
            }
            return population;
        }

        static void ConnectCities(City[] town, int[] currOrder, char[,] matrix)
        {
            
            for (int i = 0; i < currOrder.Length - 1; i++)
            {
                // Вземаме позициите и на двата "града" в реда в който са посочени последно
                int x1 = town[currOrder[i]].posX;
                int x2 = town[currOrder[i + 1]].posX;
                int y1 = town[currOrder[i]].posY;
                int y2 = town[currOrder[i + 1]].posY;

                // Проверяваме по коя от двете оси имат по-голяма разлика, за да изглежда по-добре визуализацията
                if (Math.Abs(x2 - x1) > Math.Abs(y2 - y1))
                {
                    // Използваме формулата за уравнение на права между две точки

                    for (int j = Math.Min(x1,x2) + 1; j < Math.Max(x1, x2); j++)
                    {
                        int y = (y1 + (j - x1) * (y2 - y1) / (x2 - x1));
                        matrix[j, y] = '#';
                    }
                }
                else
                {
                    for (int j = Math.Min(y1, y2) + 1; j < Math.Max(y1, y2); j++)
                    {
                        int x = (x1 + (j - y1) * (x2 - x1) / (y2 - y1));
                        matrix[x, j] = '#';
                    }
                }
            }
        }

        static int BruteForceRec(
            int[,] dist, 
            int from, 
            List<int> visited, 
            out string order)
        {
            order = "";

            if(visited.Count() + 1 == dist.GetLength(0))
            {
                order += from.ToString() + " 0";
                return dist[from, 0];
            }

            visited.Add(from);

            var bestFitness = int.MaxValue;
            for (int to = 0; to < dist.GetLength(0); to++)
            {
                if(!visited.Contains(to) && dist[from,to] != -1)
                {
                    var subFitness = dist[from, to] + BruteForceRec(dist, to, visited, out string subOrder);

                    if (subFitness < dist[from, to])
                        continue;

                    if (subFitness < bestFitness)
                    {
                        bestFitness = subFitness;
                        order = from + " " + subOrder;
                    }
                }
            }

            visited.Remove(from);

            return bestFitness == int.MaxValue
                ? -1
                : bestFitness;
        }

        static int[] ConvertStringToArray(string str)
        {
            return str.Split(' ')
                      .Select(int.Parse)
                      .ToArray();
        }

        static void ResetMatrix(char[, ] matrix)
        {
            for (int i = 0; i < matrix.GetLength(0); i++)
                for (int j = 0; j < matrix.GetLength(0); j++)
                    matrix[i, j] = '-';
        }
        
        static void Main(string[] args)
        {
            // Town settings
            int townSize = 40;
            int numCities = 25;
            Random r = new Random();

            var town = CreateCities(numCities, townSize);
            var dist = CalculateDistances(town);
            char[,] townMatrix = new char[townSize, townSize];

            DateTime timeStart;
            DateTime timeFinish;

            // Brute force
            /*
            var visited = new List<int>();
            timeStart = DateTime.Now;
            var BFFitness = BruteForceRec(dist, 0, visited, out string BFOrderString);
            int[] BFOrder = ConvertStringToArray(BFOrderString);
            timeFinish = DateTime.Now;


            ResetMatrix(townMatrix);
            ConnectCities(town, BFOrder, townMatrix);
            DrawTown(town, townMatrix, BFOrder);
            Console.WriteLine("\nDistance with BF: " + BFFitness +
                              "\nTime elapsed with BF: " + (timeFinish - timeStart));
            */

            // Population settings
            int populationSize = 10000;
            int mutationChance = 5;
            var population = new List<int[]>();

            for (int i = 0; i < populationSize; i++)
            {
                population.Add(GenerateSeed(town, r));
            }

            int[] fitness = FitnessCalc(population, dist);
            int minFitnessId = BestPaths(fitness, out int minFitnessId2);
            int[] bestPath = population[minFitnessId];
            int[] secondBestPath = population[minFitnessId2];
            int cnt = 0;
            timeStart = DateTime.Now;

            //[1,2,3,4]
            while (DateTime.Now < timeStart.AddSeconds(10))
            {
                population = GenerateNextPopulation(town, bestPath, secondBestPath, populationSize);
                Mutate(population, mutationChance);
                fitness = FitnessCalc(population, dist);
                minFitnessId = BestPaths(fitness, out minFitnessId2);
                bestPath = population[minFitnessId];
                secondBestPath = population[minFitnessId2];
                cnt++;
            }
            timeFinish = DateTime.Now;

            ResetMatrix(townMatrix);
            ConnectCities(town, bestPath, townMatrix);
            DrawTown(town, townMatrix, bestPath);

            Console.WriteLine("\nPopulation with GA: " + cnt + 
                              "\nTime elapsed with GA: " + (timeFinish - timeStart));

            Console.ReadLine();

            /* 12 града:
             * BF : 56.37 сек
             * GA :  0.06 сек
             * 13 града:
             * BF : 12 мин 15.63 сек
             * GA :  0.11 сек
             * 14 града:
             * BF : 2 часа 49 мин 18.95 сек
             * GA :  0.19 сек
             */
        }
    }
}
