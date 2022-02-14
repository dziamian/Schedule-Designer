using ScheduleDesigner.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Models
{
    /// <summary>
    /// Model danych przechowujący informacje o pojedynczej ramie czasowej.
    /// </summary>
    public class Timestamp : IExportCsv
    {
        /// <summary>
        /// Identyfikator ramy czasowej.
        /// </summary>
        [Key]
        public int TimestampId { get; set; }

        /// <summary>
        /// Indeks okienka czasowego w ciągu dnia.
        /// </summary>
        public int PeriodIndex { get; set; }

        /// <summary>
        /// Indeks dnia tygodnia.
        /// </summary>
        [Range(1,5, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
        public int Day { get; set; }

        /// <summary>
        /// Tydzień semestru, którego dotyczy rama czasowa.
        /// </summary>
        public int Week { get; set; }

        /// <summary>
        /// Reprezentacja relacji z pozycjami na planie, które odbywają się w danej ramie czasowej.
        /// </summary>
        public virtual ICollection<SchedulePosition> SchedulePositions { get; set; }

        /// <summary>
        /// Reprezentacja relacji z pojedynczymi przesunięciami, które posiadają daną docelową ramę czasową.
        /// </summary>
        public virtual ICollection<ScheduledMovePosition> ScheduledMoves { get; set; }

        /// <summary>
        /// Implementacja metody interfejsu (<see cref="IExportCsv.GetHeader"/>).
        /// Pobranie nagłówków właściwości modelu.
        /// </summary>
        /// <param name="delimiter">Ogranicznik między poszczególnymi nagłówkami</param>
        /// <returns>Pojedynczy wiersz z nagłówkami</returns>
        public string GetHeader(string delimiter)
        {
            return $"TimestampId{delimiter}PeriodIndex{delimiter}Day{delimiter}Week\n";
        }

        /// <summary>
        /// Implementacja metody interfejsu (<see cref="IExportCsv.GetRow"/>).
        /// Pobranie danych z modelu.
        /// </summary>
        /// <param name="delimiter">Ogranicznik między poszczególnymi danymi</param>
        /// <returns>Pojedynczy wiersz z danymi</returns>
        public string GetRow(string delimiter)
        {
            return $"{TimestampId}{delimiter}{PeriodIndex}{delimiter}{Day}{delimiter}{Week}\n";
        }

        public override string ToString()
        {
            return $"({PeriodIndex},{Day},{Week})";
        }
    }
}
