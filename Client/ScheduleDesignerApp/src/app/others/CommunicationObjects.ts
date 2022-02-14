/**
 * Klasa wykorzystywana do odbioru informacji o powodzeniu lub niepowodzeniu operacji wykonywanych w centrum SignalR na serwerze.
 */
export class MessageObject {
    /** Dodatkowa wiadomość informująca o statusie operacji. */
    Message:string = "";

    constructor(
        /** Kod statusu operacji. */
        public StatusCode:number
    ) {}
}

/**
 * Klasa reprezentująca pozycję w planie, która została odebrana z centrum SignalR.
 */
export class SchedulePosition {
    /** Określa czy pozycja jest zablokowana. */
    IsLocked: boolean;
    /** Określa czy pozycja jest zablokowana przez administratora. */
    IsLockedByAdmin: boolean;

    constructor(
        /** ID przedmiotu. */
        public CourseId:number,
        /** ID edycji zajęć. */
        public CourseEditionId:number,
        /** Identyfikator pokoju. */
        public RoomId:number,
        /** Indeks okienka czasowego w ciągu dnia. */
        public PeriodIndex:number,
        /** Indeks dnia tygodnia. */
        public Day:number,
        /** Tygodnie, które dotyczą pozycji w planie. */
        public Weeks:number[]
    ) {}
}

/**
 * Klasa reprezentująca obiekt przesłany przez centrum SignalR jako powiadomienie o nowo dodanych pozycjach w planie.
 */
export class AddedSchedulePositions {

    constructor(
        /** Identyfikatory grup (łącznie z nadrzędnymi i podrzędnymi), których dotyczyła operacja. */
        public GroupsIds:number[],
        /** Liczba grup głównych (bez brania pod uwagę nadrzędnych i podrzędnych). */
        public MainGroupsAmount:number,
        /** Identyfikatory prowadzących, których dotyczyła operacja. */
        public CoordinatorsIds:number[],
        /** Informacje o pozycji w planie, której dotyczyła operacja. */
        public SchedulePosition:SchedulePosition
    ) {}
}

/**
 * Klasa reprezentująca obiekt przesłany przez centrum SignalR jako powiadomienie o zmianach w planie.
 */
export class ModifiedSchedulePositions {

    constructor(
        /** Identyfikatory grup (łącznie z nadrzędnymi i podrzędnymi), których dotyczyła operacja. */
        public GroupsIds:number[],
        /** Liczba grup głównych (bez brania pod uwagę nadrzędnych i podrzędnych). */
        public MainGroupsAmount:number,
        /** Identyfikatory prowadzących, których dotyczyła operacja. */
        public CoordinatorsIds:number[],
        /** Informacje o źródłowej pozycji w planie, której dotyczyła operacja. */
        public SourceSchedulePosition:SchedulePosition,
        /** Informacje o docelowej pozycji w planie, której dotyczyła operacja. */
        public DestinationSchedulePosition:SchedulePosition,
        /** Identyfikatory usuniętych zaplanowanych ruchów. */
        public MovesIds:number[]
    ) {}
}

/**
 * Klasa reprezentująca obiekt przesłany przez centrum SignalR jako powiadomienie o usuniętych pozycjach z planu.
 */
export class RemovedSchedulePositions {

    constructor(
        /** Identyfikatory grup (łącznie z nadrzędnymi i podrzędnymi), których dotyczyła operacja. */
        public GroupsIds:number[],
        /** Liczba grup głównych (bez brania pod uwagę nadrzędnych i podrzędnych). */
        public MainGroupsAmount:number,
        /** Identyfikatory prowadzących, których dotyczyła operacja. */
        public CoordinatorsIds:number[],
        /** Informacje o pozycji w planie, której dotyczyła operacja. */
        public SchedulePosition:SchedulePosition,
        /** Identyfikatory usuniętych zaplanowanych ruchów. */
        public MovesIds:number[]
    ) {}
}