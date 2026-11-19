namespace IdleClickerPeriodicTable.Core
{
    /// <summary>Las cuentas de recursos no bajan de cero ni se salen de lo que cabe en un long.</summary>
    public static class ResourceMath
    {
        /// <summary>Se suman los recursos, si no cabe todo en un long, el resultado se queda en su máximo.</summary>
        /// <param name="current">La cantidad disponible, si es negativa se toma como cero.</param>
        /// <param name="amount">La cantidad que se añade, si es negativa se toma como cero.</param>
        /// <returns>La suma, sin superar el máximo de long.</returns>
        public static long AddCapped(long current, long amount)
        {
            if (current < 0)
                current = 0;

            if (amount < 0)
                amount = 0;

            // Se comprueba el espacio que queda antes de sumar, así un desbordamiento no vuelve negativo el recurso.
            return amount > long.MaxValue - current ? long.MaxValue : current + amount;
        }

        /// <summary>Se multiplican las cantidades sin salir del rango de long.</summary>
        /// <param name="first">El primer factor, si es cero o negativo el resultado es cero.</param>
        /// <param name="second">El segundo factor, si es cero o negativo el resultado es cero.</param>
        /// <returns>El producto, sin superar el máximo de long.</returns>
        public static long MultiplyCapped(long first, long second)
        {
            if (first <= 0 || second <= 0)
                return 0;

            // La división permite saber si el producto cabe antes de hacer la multiplicación.
            return first > long.MaxValue / second ? long.MaxValue : first * second;
        }
    }
}
