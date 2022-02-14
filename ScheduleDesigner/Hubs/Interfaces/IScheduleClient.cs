using ScheduleDesigner.Hubs.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Hubs.Interfaces
{
    /// <summary>
    /// Interfejs przeznaczony do komunikacji z połączonymi użytkownikami z centrum SignalR.
    /// </summary>
    public interface IScheduleClient
    {
        /// <summary>
        /// Funkcja powiadamiająca o zablokowaniu edycji zajęć w systemie.
        /// </summary>
        /// <param name="courseId">ID zablokowanego przedmiotu</param>
        /// <param name="courseEditionId">ID zablokowanej edycji zajęć</param>
        /// <param name="byAdmin">Czy blokada należy do administratora</param>
        /// <returns>Asynchroniczną operację</returns>
        Task LockCourseEdition(int courseId, int courseEditionId, bool byAdmin);

        /// <summary>
        /// Funkcja powiadamiająca o zablokowaniu pozycji na planie w systemie.
        /// </summary>
        /// <param name="courseId">ID przedmiotu, którego dane pozycje zostały zablokowane</param>
        /// <param name="courseEditionId">ID edycji zajęć, której dane pozycje zostały zablokowane</param>
        /// <param name="roomId">Identyfikator pokoju, którego dotyczy blokada</param>
        /// <param name="periodIndex">Indeks okienka czasowego w ciągu dnia, którego dotyczy blokada</param>
        /// <param name="day">Indeks dnia tygodnia, którego dotyczy blokada</param>
        /// <param name="weeks">Tygodnie, których dotyczy blokada</param>
        /// <param name="byAdmin">Czy blokada należy do administratora</param>
        /// <returns>Asynchroniczną operację</returns>
        Task LockSchedulePositions(
            int courseId, int courseEditionId, 
            int roomId, int periodIndex, 
            int day, int[] weeks, bool byAdmin);

        /// <summary>
        /// Funkcja powiadamiająca o odblokowaniu edycji zajęć w systemie.
        /// </summary>
        /// <param name="courseId">ID odblokowanego przedmiotu</param>
        /// <param name="courseEditionId">ID odblokowanej edycji zajęć</param>
        /// <returns>Asynchroniczną operację</returns>
        Task UnlockCourseEdition(int courseId, int courseEditionId);

        /// <summary>
        /// Funkcja powiadamiająca o odblokowaniu pozycji na planie w systemie.
        /// </summary>
        /// <param name="courseId">ID przedmiotu, którego dane pozycje zostały odblokowane</param>
        /// <param name="courseEditionId">ID edycji zajęć, której dane pozycje zostały odblokowane</param>
        /// <param name="roomId">Identyfikator pokoju, którego dotyczy odblokowanie</param>
        /// <param name="periodIndex">Indeks okienka czasowego w ciągu dnia, którego dotyczy odblokowanie</param>
        /// <param name="day">Indeks dnia tygodnia, którego dotyczy odblokowanie</param>
        /// <param name="weeks">Tygodnie, których dotyczy odblokowanie</param>
        /// <returns>Asynchroniczną operację</returns>
        Task UnlockSchedulePositions(
            int courseId, int courseEditionId, 
            int roomId, int periodIndex, 
            int day, int[] weeks);

        /// <summary>
        /// Funkcja powiadamiająca o dodaniu nowych pozycji na planie w systemie.
        /// </summary>
        /// <param name="courseId">ID przedmiotu, którego dotyczyła operacja</param>
        /// <param name="courseEditionId">ID edycji zajęć, którego dotyczyła operacja</param>
        /// <param name="groupsIds">Identyfikatory grup (łącznie z nadrzędnymi i podrzędnymi), których dotyczyła operacja</param>
        /// <param name="mainGroupsAmount">Liczba grup głównych (bez brania pod uwagę nadrzędnych i podrzędnych)</param>
        /// <param name="coordinatorsIds">Identyfikatory prowadzących (użytkowników), których dotyczyła operacja</param>
        /// <param name="roomId">Identyfikator pokoju, którego dotyczyła operacja</param>
        /// <param name="periodIndex">Indeks okienka czasowego w ciągu dnia, którego dotyczyła operacja</param>
        /// <param name="day">Indeks dnia tygodnia, którego dotyczyła operacja</param>
        /// <param name="weeks">Tygodnie, których dotyczyła operacja</param>
        /// <returns>Asynchroniczną operację</returns>
        Task AddedSchedulePositions(
            int courseId, int courseEditionId,
            int[] groupsIds, int mainGroupsAmount, int[] coordinatorsIds,
            int roomId, int periodIndex, 
            int day, int[] weeks);

        /// <summary>
        /// Funkcja powiadamiająca o przeprowadzonej zmianie na planie w systemie.
        /// </summary>
        /// <param name="courseId">ID przedmiotu, którego dotyczyła operacja</param>
        /// <param name="courseEditionId">ID edycji zajęć, którego dotyczyła operacja</param>
        /// <param name="groupsIds">Identyfikatory grup (łącznie z nadrzędnymi i podrzędnymi), których dotyczyła operacja</param>
        /// <param name="mainGroupsAmount">Liczba grup głównych (bez brania pod uwagę nadrzędnych i podrzędnych)</param>
        /// <param name="coordinatorsIds">Identyfikatory prowadzących (użytkowników), których dotyczyła operacja</param>
        /// <param name="previousRoomId">Identyfikator pokoju źródłowego, którego dotyczyła operacja</param>
        /// <param name="newRoomId">Identyfikator pokoju docelowego, którego dotyczyła operacja</param>
        /// <param name="previousPeriodIndex">Indeks źródłowego okienka czasowego w ciągu dnia, którego dotyczyła operacja</param>
        /// <param name="newPeriodIndex">Indeks docelowego okienka czasowego w ciągu dnia, którego dotyczyła operacja</param>
        /// <param name="previousDay">Indeks źródłowego dnia tygodnia, którego dotyczyła operacja</param>
        /// <param name="newDay">Indeks docelowego dnia tygodnia, którego dotyczyła operacja</param>
        /// <param name="previousWeeks">Tygodnie źródłowe, których dotyczyła operacja</param>
        /// <param name="newWeeks">Tygodnie docelowe, których dotyczyła operacja</param>
        /// <param name="movesIds">Identyfikatory usuniętych zaplanowanych ruchów</param>
        /// <returns>Asynchroniczną operację</returns>
        Task ModifiedSchedulePositions(
            int courseId, int courseEditionId,
            int[] groupsIds, int mainGroupsAmount, int[] coordinatorsIds,
            int previousRoomId, int newRoomId,
            int previousPeriodIndex, int newPeriodIndex,
            int previousDay, int newDay,
            int[] previousWeeks, int[] newWeeks,
            int[] movesIds);

        /// <summary>
        /// Funkcja powiadamiająca o usunięciu pozycji z planu w systemie.
        /// </summary>
        /// <param name="courseId">ID przedmiotu, którego dotyczyła operacja</param>
        /// <param name="courseEditionId">ID edycji zajęć, którego dotyczyła operacja</param>
        /// <param name="groupsIds">Identyfikatory grup (łącznie z nadrzędnymi i podrzędnymi), których dotyczyła operacja</param>
        /// <param name="mainGroupsAmount">Liczba grup głównych (bez brania pod uwagę nadrzędnych i podrzędnych)</param>
        /// <param name="coordinatorsIds">Identyfikatory prowadzących (użytkowników), których dotyczyła operacja</param>
        /// <param name="roomId">Identyfikator pokoju, którego dotyczyła operacja</param>
        /// <param name="periodIndex">Indeks okienka czasowego w ciągu dnia, którego dotyczyła operacja</param>
        /// <param name="day">Indeks dnia tygodnia, którego dotyczyła operacja</param>
        /// <param name="weeks">Tygodnie, których dotyczyła operacja</param>
        /// <param name="movesIds">Identyfikatory usuniętych zaplanowanych ruchów</param>
        /// <returns>Asynchroniczną operację</returns>
        Task RemovedSchedulePositions(
            int courseId, int courseEditionId,
            int[] groupsIds, int mainGroupsAmount, int[] coordinatorsIds,
            int roomId, int periodIndex,
            int day, int[] weeks,
            int[] movesIds);

        /// <summary>
        /// Funkcja powiadamiająca o dodaniu zaplanowanej zmiany do wykonania lub propozycji zmiany w systemie.
        /// </summary>
        /// <param name="moveId">Identyfikator nowo dodanego ruchu</param>
        /// <param name="userId">Identyfikator użytkownika, który dodał ruch</param>
        /// <param name="isConfirmed">Czy ruch jest potwierdzony do wykonania</param>
        /// <param name="courseId">ID przedmiotu, którego dotyczyła operacja</param>
        /// <param name="courseEditionId">ID edycji zajęć, którego dotyczyła operacja</param>
        /// <param name="roomId">Identyfikator pokoju, którego dotyczyła operacja</param>
        /// <param name="periodIndex">Indeks okienka czasowego w ciągu dnia, którego dotyczyła operacja</param>
        /// <param name="day">Indeks dnia tygodnia, którego dotyczyła operacja</param>
        /// <param name="weeks">Tygodnie, których dotyczyła operacja</param>
        /// <returns>Asynchroniczną operację</returns>
        Task AddedScheduledMove(
            int moveId, int userId, bool isConfirmed,
            int courseId, int courseEditionId,
            int roomId, int periodIndex,
            int day, int[] weeks);

        /// <summary>
        /// Funkcja powiadamiająca o cofnięciu zaplanowanej zmiany do wykonania lub propozycji zmiany w systemie.
        /// </summary>
        /// <param name="moveId">Identyfikator cofniętego ruchu</param>
        /// <param name="courseId">ID przedmiotu, którego dotyczyła operacja</param>
        /// <param name="courseEditionId">ID edycji zajęć, którego dotyczyła operacja</param>
        /// <param name="roomId">Identyfikator pokoju, którego dotyczyła operacja</param>
        /// <param name="periodIndex">Indeks okienka czasowego w ciągu dnia, którego dotyczyła operacja</param>
        /// <param name="day">Indeks dnia tygodnia, którego dotyczyła operacja</param>
        /// <param name="weeks">Tygodnie, których dotyczyła operacja</param>
        /// <returns>Asynchroniczną operację</returns>
        Task RemovedScheduledMove(
            int moveId,
            int courseId, int courseEditionId,
            int roomId, int periodIndex,
            int day, int[] weeks);

        /// <summary>
        /// Funkcja powiadamiająca o zaakceptowaniu propozycji zmiany w systemie.
        /// </summary>
        /// <param name="moveId">Identyfikator ruchu, którego dotyczyła operacja</param>
        /// <param name="courseId">ID przedmiotu, którego dotyczyła operacja</param>
        /// <param name="courseEditionId">ID edycji zajęć, którego dotyczyła operacja</param>
        /// <param name="roomId">Identyfikator pokoju, którego dotyczyła operacja</param>
        /// <param name="periodIndex">Indeks okienka czasowego w ciągu dnia, którego dotyczyła operacja</param>
        /// <param name="day">Indeks dnia tygodnia, którego dotyczyła operacja</param>
        /// <param name="weeks">Tygodnie, których dotyczyła operacja</param>
        /// <returns></returns>
        Task AcceptedScheduledMove(
            int moveId,
            int courseId, int courseEditionId,
            int roomId, int periodIndex,
            int day, int[] weeks);

        /// <summary>
        /// Funkcja powiadamiająca użytkownika, który wykonał operację o jej statusie.
        /// </summary>
        /// <param name="response">Obiekt z informacją o statusie operacji</param>
        /// <returns></returns>
        Task SendResponse(MessageObject response);
    }
}
