using System;
using System.ComponentModel;
using System.Xml.Serialization;

class SimplexMethod
{
    static double M = 1000;
    static int n = 2;//число указывающее на кол-во переменных в начальном уравнении
    static void Main()
    {
        // Пример задачи линейного программирования в канонической форме:
        // Максимизировать Z = 2x1 - x2
        // При ограничениях:
        // x1 - x2 >= -3
        // 6x1 + 7x2 <= 42
        // 2x1 - 3x2 <= 6
        // x1 + x2 >= 4
        // x1, x2 >= 0 ( это по стандарту)

        //Если у вас m - ограничений, то массив Barrier_tableau, должен быть размерности [n+2m+2][m],
        //где n - это кол-во начальных переменных в главной функции (у нас 2).
        //Это сделана для учёта исскуственных переменных.
        //В Main_Function размерность состовляет [n+2m+1]

        double[] Main_Function = {2, -1, 0, 0, 0, 0, 0, 0, 0, 0, 1 };//последний столбец это тип экстермума -1 == min; 1 == max(никак не применяется, можно добавить функционал)

        double[,] Barrier_tableau = {
            {1, -1, 0, 0, 0, 0, 0, 0, 0, 0, -3,1},//задаём с учетом исскуственных переменных
            {6, 7, 0, 0, 0, 0, 0, 0, 0, 0, 42,-1},// пред-последний столюец это праве значение неравенства
            {2, -3, 0, 0, 0, 0, 0, 0, 0, 0, 6,-1},// последний столбец это тип неравенства -1 == <=; 0 == =; 1 == >=
            {1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 4,1}
        };

        DisplayTableauX2(Barrier_tableau, "Начальная таблица:");
        DisplayTableauX1(Main_Function, "Начальная функция:");

        Prepare(Barrier_tableau, Main_Function);
        DisplayTableauX2(Barrier_tableau, "Преобразованная таблица:");
        DisplayTableauX1(Main_Function, "Преобразованная функция:");

        DisplayTableauX2(CreateUltimateTableau(Barrier_tableau, Main_Function), "Преобразованная таблица:");

        DisplayTableauX2(Solve(CreateUltimateTableau(Barrier_tableau, Main_Function), Main_Function), "Конечная иттерация");

        Console.ReadLine();
    }

    static double[,] Solve(double[,] UltimateTableau, double[] Main_Function)
    {
        //дозаписываем таблицу (рассчитываем 0-ю строку)
        for (int j = 1; j < UltimateTableau.GetLength(1); j++)
        {
            for (int k = 1; k < UltimateTableau.GetLength(0); k++)
                UltimateTableau[0, j] += UltimateTableau[k, j] * Main_Function[(int)UltimateTableau[k, 0] - 1];
            if (j != 1)
                UltimateTableau[0, j] -= Main_Function[j-2];
        }

        int count = 0;
        DisplayTableauX2(UltimateTableau, $"Иттерация: {count}");

        while (HasNegative(UltimateTableau))
        {
            int enteringVariable = FindEnteringVariable(UltimateTableau);
            if (enteringVariable == -1)
                break; // Если не найдена входящая переменная, завершаем цикл

            int leavingVariable = FindLeavingVariable(UltimateTableau, enteringVariable);
            if (leavingVariable == -1)
                break; // Если не найдена исходящая переменная, завершаем цикл

            UpdateTableau(UltimateTableau, leavingVariable, enteringVariable);
            count++;
            DisplayTableauX2(UltimateTableau, $"Иттерация: {count}");
        }

        double[,] AnswerTabel = new double[n,2];
        int AnswerCount = 0;
        
        for (int i = 1; i < UltimateTableau.GetLength(0); i++)//Запись ответов в массив ответов
        {
            
            if (UltimateTableau[i, 0] <= n)
            {
                AnswerTabel[AnswerCount, 0] = UltimateTableau[i, 0];
                AnswerTabel[AnswerCount, 1] = UltimateTableau[i, 1];
                AnswerCount++;
            }
        }
        
        DisplayTableauX2(AnswerTabel, "Значения перменных(ответ)");
        double Function = 0;
        for (int i = 0; i < AnswerTabel.GetLength(0); i++)
            Function += Main_Function[i] * AnswerTabel[i, 1];

        Console.WriteLine("Значении функции в точке максимума: " + Function);

        return UltimateTableau;
    }

    static bool HasNegative(double[,] tableau)
    {
        int lastCol = tableau.GetLength(1);
        for (int j = 1; j < lastCol; j++)
        {
            if (tableau[0, j] < 0)
                return true;
        }
        return false;
    }

    static double[,] CreateUltimateTableau(double[,] Barrier_tableau, double[] Main_Function)
    {
        double checkCount = 0;
        int count = 0;
        for (int j = 0; j < Barrier_tableau.GetLength(1); j++)//в теории можно вынести блок
        {
            for (int i = 0; i < Barrier_tableau.GetLength(0); i++)
                checkCount += Math.Abs(Barrier_tableau[i, j]);
            if (checkCount != 0)
            {
                count++;
                checkCount = 0;
            }
            else break;
        }

        double[,] UltimateTableu = new double[Barrier_tableau.GetLength(0)+1, count+2];

        for (int i = 1; i < UltimateTableu.GetLength(0); i++)
        {
            UltimateTableu[i, 1] = Barrier_tableau[i-1, Barrier_tableau.GetLength(1)-2];//заполнение 2-й строки 
            for (int j = 2; j < UltimateTableu.GetLength(1); j++) // заполнение остальной части
            {
                UltimateTableu[i,j] = Barrier_tableau[i-1,j-2];
            }
        }

        // заполнение строки с индексами переменных, нахождение знака с конца и присвоение индекса
        for (int i = 1; i < UltimateTableu.GetLength(0); i++)
        {
             for (int j = UltimateTableu.GetLength(1)-1; j > 1 + n; j--)
                {
                if (UltimateTableu[i, j] == 1)
                {
                    UltimateTableu[i, 0] = j-1;
                    break;
                }
                    
            }
            
        }


        return UltimateTableu;
    }

    static double[,] Prepare(double[,] Barrier_tableau, double[] Main_Function)
    {
        int rows = Barrier_tableau.GetLength(0);
        int cols = Barrier_tableau.GetLength(1)-1;
        
        int count = n-1;// для добавления новых переменных
        
        for (int i = 0; i < rows; i++) { 
            if (Barrier_tableau[i, cols] == 1 & Barrier_tableau[i, cols - 1] < 0)
            {
                Barrier_tableau = ChangeOfSign(Barrier_tableau,i);
                count++;
                Barrier_tableau[i, count] = 1;
                Barrier_tableau[i, cols] = 0;
            }
            if(Barrier_tableau[i, cols] == 1 & Barrier_tableau[i, cols - 1] > 0)
            {
                count += 2;
                Barrier_tableau[i, count - 1] = -1;
                Barrier_tableau[i,count] = 1;//Исскуственная переменная
                Main_Function[count] = -M;
                Barrier_tableau[i, cols] = 0;
            }
            if(Barrier_tableau[i, cols] == -1 & Barrier_tableau[i, cols - 1] < 0)
            {
                Barrier_tableau = ChangeOfSign(Barrier_tableau, i);
                count += 2;
                Barrier_tableau[i, count - 1] = -1;
                Barrier_tableau[i, count] = 1;//Исскуственная переменная
                Main_Function[count] = -M;
                Barrier_tableau[i, cols] = 0;
            }
            if (Barrier_tableau[i, cols] == -1 & Barrier_tableau[i, cols - 1] > 0)
            {
                count++;
                Barrier_tableau[i, count] = 1;
                Barrier_tableau[i, cols] = 0;
            }

        }
        return Barrier_tableau;
    }

    static double[,] ChangeOfSign(double[,] Barrier_tableau,int index)
    {
        int cols = Barrier_tableau.GetLength(1);
        for (int j = 0; j < cols; j++)
            Barrier_tableau[index, j] *= -1;

        return Barrier_tableau;
    }

    static int FindEnteringVariable(double[,] tableau)
    {
        int lastCol = tableau.GetLength(1);
        double minValue = 0;
        int indexOfmin = -1;

        for (int j = 2; j < lastCol; j++)
        {
            if (tableau[0, j] < 0 & tableau[0, j] < minValue)
            {
                minValue = tableau[0, j];
                indexOfmin = j;
            }
                
        }
        if (minValue != 0)
            return indexOfmin;
        return -1;
    }

    static int FindLeavingVariable(double[,] tableau, int pivotColumn)
    {
        int lastRow = tableau.GetLength(0);

        double minRatio = double.MaxValue;
        int pivotRow = -1;

        for (int i = 1; i < lastRow; i++)
        {
            if (tableau[i, pivotColumn] > 0)
            {
                double ratio = tableau[i, 1] / tableau[i, pivotColumn];
                if (ratio < minRatio)
                {
                    minRatio = ratio;
                    pivotRow = i;
                }
            }
        }

        return pivotRow;
    }

    static void UpdateTableau(double[,] tableau, int pivotRow, int pivotColumn)
    {
        int rows = tableau.GetLength(0);
        int cols = tableau.GetLength(1);
        //перевод в правильный столбец и строку

        double pivotElement = tableau[pivotRow, pivotColumn];

        for (int j = 1; j < cols; j++)
        {
            tableau[pivotRow, j] /= pivotElement;
        }

        for (int i = 0; i < rows; i++)
        {
            if (i != pivotRow)
            {
                double ratio = tableau[i, pivotColumn];
                for (int j = 1; j < cols; j++)
                {
                    tableau[i, j] -= ratio * tableau[pivotRow, j];
                }
            }
        }

        tableau[pivotRow, 0] = pivotColumn - 1;
    }
    public static void DisplayTableauX2(double[,] tableau, string message)
    {
        Console.WriteLine(message);
        for (int i = 0; i < tableau.GetLength(0); i++)
        {
            for (int j = 0; j < tableau.GetLength(1); j++)
            {
                Console.Write($"{tableau[i, j],8:F2} ");
            }
            Console.WriteLine();
        }
        Console.WriteLine();
    }

     static void DisplayTableauX1(double[] tableau, string message)
    {
        Console.WriteLine(message);
        for (int i = 0; i < tableau.Length; i++)
        {
            Console.Write($"{tableau[i],8:F2} ");
        }
        Console.WriteLine();
        Console.WriteLine();
    }
}