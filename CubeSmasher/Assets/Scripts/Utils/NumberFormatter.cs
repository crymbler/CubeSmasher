using UnityEngine;

public static class NumberFormatter
{
    // Метод принимает твой баланс или цену (double) и возвращает красивый текст
    public static string Format(double value)
    {
        // Для триллионов (Trillions)
        if (value >= 1_000_000_000_000) 
            return (value / 1_000_000_000_000D).ToString("0.##") + "T"; 

        // Для миллиардов (Billions)
        if (value >= 1_000_000_000) 
            return (value / 1_000_000_000D).ToString("0.##") + "B"; 

        // Для миллионов (Millions)
        if (value >= 1_000_000) 
            return (value / 1_000_000D).ToString("0.##") + "M"; 

        // Для тысяч (Kilo)
        if (value >= 1_000) 
            return (value / 1_000D).ToString("0.##") + "K"; 

        // Если число меньше 1000, выводим как есть (целым числом)
        return value.ToString("0"); 
    }
}