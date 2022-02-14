/**
 * Klasa przechowująca podstawowe informacje o grupie.
 */
export class Group {
    /** Pełna nazwa grupy do wyświetlenia w planie. */
    FullName:string;
    /** Identyfikator grupy nadrzędnej. */
    ParentGroupId:number|null;

    constructor(
        /** Identyfikator grupy. */
        public GroupId:number,
    ) { }
}

/**
 * Klasa przechowująca większą liczbę informacji o grupie. 
 */
export class GroupInfo {

    constructor(
        /** Identyfikator grupy. */
        public GroupId:number,
        /** Podstawowa nazwa grupy. */
        public BasicName:string,
        /** Pełna nazwa grupy. */
        public FullName:string,
        /** Identyfikatory grup nadrzędnych. */
        public ParentIds:number[],
        /** Identyfikatory grup podrzędnych. */
        public ChildIds:number[]
    ) { }
}