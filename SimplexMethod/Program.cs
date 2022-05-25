using System;

namespace SimplexMethod
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Нажмите Enter для решения задачи линейного программирования симлпексным методом");
            Console.ReadLine();
            Console.WriteLine("Будьте внимательны! Программа не защищена от ошибок ввода\n");
            Console.WriteLine("Введите max, если решаете задачу максимизации; min, если решаете задачу минимизации: ");
            string method;
            bool isMaximization = true;
            method = Console.ReadLine();
            if (method == "max")
                 isMaximization = true;
            if (method == "min")
                isMaximization = false;
            //Console.WriteLine("Укажите наибольшее количество переменных в ограничениях: ");

            double[] inputC = new double[] { 1, -4, 1, 1, 1, 1 }; //коэффициенты целевой функции
            double[,] inputX = new double[,] { 
                                                { 1, 1, 1, 1, -1, -1 },
                                                { 0, 1, 1, -1, -1, -1 },
                                                { 0, 1, 0, 0, 0, -1 }
                                             };
            string[] conditionsInput = new string[] { "=", "=", "=" };
            double[] inputB = new double[] { 1, 1, 2 }; 

            var simplex = new Simplex(inputC, inputX, conditionsInput, inputB, isMaximization);
            var coeffsMatrix = simplex._coeffsMatrix;
            var simplexDifference = simplex._simplexDifference;
            var basicIndexes = simplex._basicIndexes;
            var coeffsValues = simplex._coeffsValues;
            var targetValue = simplex._targetValue;
            var iterations = simplex._iterations;
            var finallyLength = simplex._finallyLength;

            //Вывод решения
            Console.Write("\n+");
            for (int i = 0; i < 80; i++)
            {
                Console.Write("-");
            }
            Console.Write("+\n");

            if (simplex.isUnlimited)
            {
                Console.WriteLine("\nЦелевая функция не ограничена. Решений нет.");
                return;
            }

            if (simplex.isIncompatibility)
            {
                Console.WriteLine("\nСистема ограничений несовместна. Решений нет.");
            }

            if (simplex.isAlternative)
            {
                Console.WriteLine("\nОбнаружен альтернативный оптимум. Дальнейшая оптимизация нецелесообразна.\n");
            }

            Console.WriteLine("Итоговый план:");
            Console.WriteLine("Количество итераций: " + iterations + "\n");
            Console.WriteLine("Базис В   Матрица коэффициентов");

            for (int j = 0; j < inputB.Length; j++)
            {
                basicIndexes[j] += 1;
                Console.Write("X" + basicIndexes[j] + "    ");
                Console.Write(Math.Round(coeffsValues[j], 2) + "   ");
                for (int i = 0; i < finallyLength; i++)
                {
                    
                    Console.Write(Math.Round(coeffsMatrix[j, i], 2));
                    Console.Write("   ");
                }
                Console.WriteLine("");
            }
            Console.WriteLine("\nСимплексная разность: ");
            for (int i = 0; i < finallyLength; i++)
            {
                Console.Write(Math.Round(simplexDifference[i], 2));
                Console.Write("   ");
            }
            Console.WriteLine("\nZ = " + Math.Round(targetValue, 2));

            //Вывод решения двойств задачи

            return;
        }
    }

    public class Simplex
    {
        public double[,] _coeffsMatrix;
        public double[] _simplexDifference;
        public int[] _basicIndexes;
        public double[] _coeffsValues;
        public double _targetValue;
        public int _iterations;
        public int _finallyLength;

        public bool isUnlimited;
        public bool isIncompatibility;
        public bool isAlternative;


        public Simplex(double[] targetCoeffs, double[,] constraintsCoeffs,
            string[] constraintsSigns, double[] constraintsValues, bool isMaximization)
        {
            int inputLength = constraintsCoeffs.GetLength(1); //количество коэффициентов в строках
            int height = constraintsCoeffs.GetLength(0); //количество коэффициентов в столбцах
            int basicsCount = 0;
            int artificialCount = 0;
            bool thereIsArtificialVar = false;          

            // ЗДЕСЬ НУЖНА ПРОВЕРКА НА МАКСИМИЗАЦИЮ ИЛИ МИНИМИЗАЦИЮ
            // Приведение В к положительным значениям
            for (int i = 0; i < height; i++)
                if (constraintsValues[i] < 0)
                {
                    if (constraintsSigns[i] == ">=")
                        constraintsSigns[i] = "<=";
                    if (constraintsSigns[i] == "<=")
                        constraintsSigns[i] = ">=";
                    constraintsValues[i] *= -1;
                    for (int j = 0; j < inputLength; j++)
                        constraintsCoeffs[i, j] *= -1;
                }
            //Преобразует матрицу коэффициентов
            for (int i = 0; i < height; i++)
            {
                if (constraintsSigns[i] == "<=" || constraintsSigns[i] == ">=")
                    basicsCount++; //Для определения, сколько нужно ввести базисных переменных
                if (constraintsSigns[i] == ">=" || constraintsSigns[i] == "=")
                {
                    thereIsArtificialVar = true;
                    artificialCount++;
                }
            }

            _finallyLength = inputLength + basicsCount + artificialCount;
            int[] basicIndexes = new int[height]; //индексы базисных переменных внутри матрицы коэффициентов

            ResizeArray(ref constraintsCoeffs, height, inputLength + basicsCount + artificialCount); //Увеличение матрицы коэффициентов на
                                                                                                     //количество базисных переменных

            for (int i = inputLength, j = 0; i < inputLength + basicsCount; i++, j++) //Запись базисных переменных в матрицу коэффициентов
            {
                if (constraintsSigns[j] == ">=")
                    constraintsCoeffs[j, i] = -1;
                if (constraintsSigns[j] == "<=")
                {
                    constraintsCoeffs[j, i] = 1;
                    basicIndexes[j] = i; //запись индексов базисных переменных внутри матрицы коэффициентов
                }
            }

            for (int i = inputLength + basicsCount, j = 0; i < inputLength + basicsCount + artificialCount; i++, j++) //Ввод искусственных переменных,
                                                                                                        //называются также базисными
            {
                if (constraintsSigns[j] == ">=" || constraintsSigns[j] == "=")
                {
                    constraintsCoeffs[j, i] = 1;
                    basicIndexes[j] = i;
                }
                else
                    i--;
            }

            ResizeArray(ref targetCoeffs, inputLength + basicsCount + artificialCount); //Добавление пустых ячеек в целевую функция до
                                                                              //длины матрицы коэффициентов
                                                                              // ЗДЕСЬ НУЖНА ПРОВЕРКА НА МАКСИМИЗАЦИЮ ИЛИ МИНИМИЗАЦИЮ
                                                                              // Поиск начального целевого вектора (обычно пишем над матрицей коэффициентов)
            if (thereIsArtificialVar) // Если в задаче был знак больше, то для максимизации необходимо ввести числа -M
                for (int i = 0; i < artificialCount; i++)
                    if (isMaximization)
                        targetCoeffs[i + basicsCount + inputLength] = -1000000;
                    else targetCoeffs[i + basicsCount + inputLength] = 1000000;

            //Расчет целевого значения
            double targetValue = 0;

            for (int i = 0; i < height; i++)
                targetValue = targetValue + targetCoeffs[basicIndexes[i]] * constraintsValues[i];

            double[] simplexDifference = new double[inputLength + basicsCount + artificialCount];

            //Поиск симплексной разницы (нижняя строка таблицы)
            for (int i = 0; i < simplexDifference.Length; i++)
            {
                for (int j = 0; j < height; j++)
                    simplexDifference[i] += targetCoeffs[basicIndexes[j]] * constraintsCoeffs[j, i];
                simplexDifference[i] -= targetCoeffs[i];
            }

            int mainColumnIndex = FindMainColumn(simplexDifference, isMaximization);
            int mainLineIndex = FindMainLine(height, constraintsValues, constraintsCoeffs, mainColumnIndex);

            //ОСНОВНОЙ АЛГОРИТМ
            //Пересчет всех значений до тех пор, пока не будут выполнены условия
            bool isNotEnd = true;
            int iterations = 0;
            while (iterations != 2)
            {               
                    OneIteration(ref constraintsCoeffs, ref simplexDifference, ref basicIndexes, height, ref constraintsValues,
                        inputLength, basicsCount, ref targetValue, ref targetCoeffs, ref isAlternative,
                        ref isIncompatibility, ref isUnlimited, basicsCount, artificialCount, isMaximization);

                    if (isAlternative)
                        return;
                    if (isIncompatibility)
                        return;
                    if (isUnlimited)
                        return;

                    _coeffsMatrix = constraintsCoeffs;
                    _simplexDifference = simplexDifference;
                    _basicIndexes = basicIndexes;
                    _coeffsValues = constraintsValues;
                    _targetValue = targetValue;
                    _iterations = iterations;
                    iterations++;

                for (int i = 0; i < inputLength + basicsCount + artificialCount; i++)
                {
                    if (simplexDifference[i] >= 0)
                    {
                        if (i == inputLength + basicsCount + artificialCount - 1)
                        {
                            var minBuffer = new double[height];
                            for (int j = 0; j < height; j++)
                                minBuffer[j] = constraintsValues[j] / constraintsCoeffs[j, mainColumnIndex];

                            double min = double.MaxValue;
                            for (int j = 0; j < height; j++)
                                if (minBuffer[j] == min)
                                    Console.WriteLine("Обнаружено вырожденное решение.");
                            isNotEnd = false;
                        }
                    }
                    else break;
                }
                //организовать выход в main функцию
            }
        }

        //Метод для решения задачи максимизации
        public static void OneIteration(ref double [,] constraintsCoeffs, ref double[] simplexDifference, ref int[] basicIndexes, 
            int height, ref double[] constraintsValues, int inputLength, int countToAdd, ref double targetValue, 
            ref double[] targetCoeffs, ref bool isAlternative, ref bool isIncompatibility, ref bool isUnlimited,
            int basicsCount, int artificialCount, bool isMaximization)
        {           
            int mainColumnIndex = FindMainColumn(simplexDifference, isMaximization);
            int mainLineIndex = FindMainLine(height, constraintsValues, constraintsCoeffs, mainColumnIndex);
            targetValue = 0;
            for(int i = 0; i < inputLength + basicsCount + artificialCount; i++)
                simplexDifference[i] = 0;

            if (mainLineIndex == -1)
            {
                isUnlimited = true;
                return;
            }
            
            //Пересчет массива B
            for (int j = 0; j < height; j++)
            {
                if (j == mainLineIndex) //По ведущей строке
                    continue;
                //По остальным строкам
                else
                {
                    constraintsValues[j] = constraintsValues[j] - (constraintsCoeffs[j, mainColumnIndex] *
                        constraintsValues[mainLineIndex] / constraintsCoeffs[mainLineIndex, mainColumnIndex]);                   
                }
            }
            //Пересчет по ведущей строке          
            constraintsValues[mainLineIndex] = constraintsValues[mainLineIndex] / constraintsCoeffs[mainLineIndex, mainColumnIndex]; 

            //Проверка на несовместность
            for (int j = 0; j < height; j++)
                if (constraintsValues[j] < 0)
                {
                    isIncompatibility = true;
                    return;
                }

            //Пересчет коэффициентов по правилу прямоугольника
            for (int i = 0; i < inputLength + basicsCount + artificialCount; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    if (i == mainColumnIndex)
                        continue;
                    else
                    {
                        if (j == mainLineIndex) //для ведущей строки
                            continue;
                        //По остальным строкам
                        else
                        {
                            constraintsCoeffs[j, i] = constraintsCoeffs[j, i] - (constraintsCoeffs[j, mainColumnIndex] *
                                    constraintsCoeffs[mainLineIndex, i] / constraintsCoeffs[mainLineIndex, mainColumnIndex]);
                        }
                    }
                }
            }
            for (int i = 0; i< inputLength + basicsCount + artificialCount; i++)
            {
                if (i == mainColumnIndex)
                    continue;
                constraintsCoeffs[mainLineIndex, i] = constraintsCoeffs[mainLineIndex, i] 
                    / constraintsCoeffs[mainLineIndex, mainColumnIndex];
            }
            //Пересчет ведущего столбца
            for (int i = 0; i < height; i++)
            {
                if (i == mainLineIndex)
                    constraintsCoeffs[mainLineIndex, mainColumnIndex] = 1;
                else constraintsCoeffs[i, mainColumnIndex] = 0;
            }

            //Указание нового базиса
            basicIndexes[mainLineIndex] = mainColumnIndex;

            //Пересчет целевой функции
            for (int i = 0; i < height; i++)
            {
                targetValue = targetValue + targetCoeffs[basicIndexes[i]] * constraintsValues[i];
            }

            //пересчет симплексной разности
            for (int i = 0; i < simplexDifference.Length; i++)
            {
                for (int j = 0; j < height; j++)
                    simplexDifference[i] += targetCoeffs[basicIndexes[j]] * constraintsCoeffs[j, i];
                simplexDifference[i] -= targetCoeffs[i];
            }

            //проверка на альтернативный оптимум
            int buf = 0;
            for (int j = 0; j < inputLength + basicsCount + artificialCount; j++)                           
                if (simplexDifference[j] >= 0)
                    buf++;                               

            if(buf == inputLength + basicsCount + artificialCount)
                for (int i = 0; i < height; i++)
                {
                    if (simplexDifference[basicIndexes[i]] == 0)
                    {
                        if (i == height - 1)
                        {
                            isAlternative = true;
                            return;
                        }
                        continue;
                    }
                    else break;
                }            
        }

        private static void ResizeArray(ref double[,] arr, int newM, int newN)
        {
            double[,] newArray = new double[newM, newN];
            for (int m = 0; m < arr.GetLength(0); m++)
                for (int n = 0; n < arr.GetLength(1); n++)
                    newArray[m, n] = arr[m, n];
            arr = newArray;
        }

        private static void ResizeArray(ref double[] arr, int newN)
        {
            double[] newArray = new double[newN];
            for (int n = 0; n < arr.Length; n++)
                newArray[n] = arr[n];
            arr = newArray;
        }

        private static int FindMainColumn(double[] simplexDifference, bool isMaximization)
        {
            int columnIndex = 0;
            double min = double.MaxValue;
            double max = double.MinValue;
            for (int i = 0; i < simplexDifference.Length; i++)
            {
                if (isMaximization)
                {
                    if (min > simplexDifference[i])
                    {
                        min = simplexDifference[i];
                        columnIndex = i;
                    }
                }
                else
                {
                    if (max < simplexDifference[i])
                    {
                        max = simplexDifference[i];
                        columnIndex = i;
                    }
                }
            }
            return columnIndex;
        }

        private static int FindMainLine(int height, double[] constraintsValues, 
            double[,] constraintsCoeffs, int mainColumnIndex)
        {
            int lineIndex = -1;
            var minBuffer = new double[height];
            for (int i = 0; i < height; i++)
                minBuffer[i] = constraintsValues[i] / constraintsCoeffs[i, mainColumnIndex];

            double min = double.MaxValue;
            for (int i = 0; i < height; i++)
            {
                if (minBuffer[i] <= 0 || double.IsInfinity(minBuffer[i]))
                {
                    continue;
                }
                else if (min > minBuffer[i])
                {
                    min = minBuffer[i];
                    lineIndex = i;
                }
            }
            return lineIndex;
        } 
    }
} 

