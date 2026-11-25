using System;
using System.Globalization;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.Serialization;

namespace IdleClickerPeriodicTable.Core
{
    /// <summary>Los recursos y el progreso viajan con la partida, aunque cambie la pantalla.</summary>
    [DataContract]
    public sealed class GameState
    {
        /// <summary>El dinero disponible para comprar y pagar el mantenimiento, siempre en unidades enteras.</summary>
        public BigInteger money;

        /// <summary>Ciencia acumulada por el trabajo de los investigadores.</summary>
        public BigInteger science;

        /// <summary>Ingenio acumulado por el trabajo de los ingenieros.</summary>
        public BigInteger ingenuity;

        /// <summary>El dinero se guarda como texto decimal para no perder ningún dígito.</summary>
        [DataMember(Name = "money", IsRequired = true)]
        string SavedMoney
        {
            get => money.ToString(CultureInfo.InvariantCulture);
            set => money = BigInteger.Parse(value, NumberStyles.None, CultureInfo.InvariantCulture);
        }

        /// <summary>La ciencia se guarda como texto decimal para no perder ningún dígito.</summary>
        [DataMember(Name = "science", IsRequired = true)]
        string SavedScience
        {
            get => science.ToString(CultureInfo.InvariantCulture);
            set => science = BigInteger.Parse(value, NumberStyles.None, CultureInfo.InvariantCulture);
        }

        /// <summary>El ingenio se guarda como texto decimal para no perder ningún dígito.</summary>
        [DataMember(Name = "ingenuity", IsRequired = true)]
        string SavedIngenuity
        {
            get => ingenuity.ToString(CultureInfo.InvariantCulture);
            set => ingenuity = BigInteger.Parse(value, NumberStyles.None, CultureInfo.InvariantCulture);
        }

        /// <summary>Los investigadores contratados, cada uno produce según el nivel de su profesión.</summary>
        [DataMember] public int researchers;

        /// <summary>Los ingenieros contratados, cada uno produce según el nivel de su profesión.</summary>
        [DataMember] public int engineers;

        /// <summary>El nivel aplicado a Investigación, puede faltar al cargar una partida anterior a estas mejoras.</summary>
        [DataMember(EmitDefaultValue = false)] int researchStaffLevel;

        /// <summary>El nivel aplicado a Ingeniería, puede faltar al cargar una partida anterior a estas mejoras.</summary>
        [DataMember(EmitDefaultValue = false)] int engineeringStaffLevel;

        /// <summary>El nivel de Investigación queda entre 1 y 5, las partidas que no lo guardaban empiezan en 1.</summary>
        public int ResearchStaffLevel => Math.Clamp(researchStaffLevel, 1, 5);

        /// <summary>El nivel de Ingeniería queda entre 1 y 5, las partidas que no lo guardaban empiezan en 1.</summary>
        public int EngineeringStaffLevel => Math.Clamp(engineeringStaffLevel, 1, 5);

        /// <summary>El nuevo nivel se aplica después de fabricar la mejora de esa profesión.</summary>
        internal void ApplyStaffLevel(StaffProfession profession, int level)
        {
            if (profession == StaffProfession.Research)
                researchStaffLevel = level;
            else if (profession == StaffProfession.Engineering)
                engineeringStaffLevel = level;
        }

        /// <summary>Los investigadores pausados conservan su contratación, pero no cobran ni producen.</summary>
        [DataMember] public bool researchersPaused;

        /// <summary>Los ingenieros pausados conservan su contratación, pero no cobran ni producen.</summary>
        [DataMember] public bool engineersPaused;

        /// <summary>Los modelos descubiertos no se pierden aunque después se gaste la ciencia que permitió encontrarlos.</summary>
        [DataMember] public int discoveryLevel;

        /// <summary>Investigaciones desbloqueadas hasta ahora, entre cero y cuatro.</summary>
        [DataMember] public int researchLevel;

        /// <summary>Cantidad de máquinas disponibles en cada una de las cuatro familias.</summary>
        [DataMember(IsRequired = true)] public int[] machines = new int[4];

        /// <summary>Lo que se multiplica la producción de cada familia, de uno a cinco.</summary>
        [DataMember(IsRequired = true)] public int[] machineLevels = { 1, 1, 1, 1 };

        /// <summary>Los descubrimientos de planos y personal, este progreso puede faltar en una partida antigua.</summary>
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, ProductionProgress> productionProgress;
    }
}
