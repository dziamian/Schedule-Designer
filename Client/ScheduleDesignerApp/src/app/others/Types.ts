/**
 * Klasa reprezentująca typ przedmiotu.
 */
export class CourseType {
    
    constructor(
        /** ID typu przedmiotu. */
        public CourseTypeId:number,
        /** Nazwa typu przedmiotu. */
        public Name:string,
        /** Kolor typu przedmiotu (wyświetlany jako tło panelu zajęć). */
        public Color:string
    ) {}
}

/**
 * Klasa reprezentująca typ pokoju.
 */
export class RoomType {

    constructor(
        /** ID typu pokoju. */
        public RoomTypeId:number,
        /** Nazwa typu pokoju. */
        public Name:string
    ) {}
}