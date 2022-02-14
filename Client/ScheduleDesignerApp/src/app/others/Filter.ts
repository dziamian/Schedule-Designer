/**
 * Klasa reprezentująca filtr wykorzystywany do wyświetlania planu zajęć dla wybranego zasobu.
 */
export class Filter {
    
    constructor(
        /** Identyfikatory prowadzących. */
        public CoordinatorsIds: number[],
        /** Identyfikatory grup. */
        public GroupsIds: number[],
        /** Identyfikatory pokojów. */
        public RoomsIds: number[]
    ) {}

    /**
     * Sprawdza czy dwa filtry posiadają wspólne wartości.
     * @param filter Filtr do porównania
     * @returns Prawdę, jeśli co najmniej jedna wartość jest wspólna dla obu filtrów, w przeciwnym wypadku fałsz
     */
    public challengeAll(filter: Filter): boolean {
        const coordinatorsLength = this.CoordinatorsIds.length;
        const groupsLength = this.GroupsIds.length;
        const roomsLength = this.RoomsIds.length;
        
        if (coordinatorsLength == 0 && groupsLength == 0 && roomsLength == 0) {
            return true;
        }
        
        if (coordinatorsLength > 0 && this.CoordinatorsIds.some(e => filter.CoordinatorsIds.includes(e))) {
            return true;
        }
        if (groupsLength > 0 && this.GroupsIds.some(e => filter.GroupsIds.includes(e))) {
            return true;
        }
        if (roomsLength > 0 && this.RoomsIds.some(e => filter.RoomsIds.includes(e))) {
            return true;
        }
        return false;
    }

    /**
     * Sprwadza czy filtr posiada identyfikator pokoju.
     * @param roomId Identyfikator pokoju
     * @returns Prawdę, jeśli posiada identyfikator pokoju, w przeciwnym wypadku fałsz.
     */
    public challengeRoom(roomId: number): boolean {
        const roomsLength = this.RoomsIds.length;

        if (roomsLength == 0) {
            return true;
        }

        if (roomsLength > 0 && this.RoomsIds.some(e => e == roomId)) {
            return true;
        }
        return false;
    }

    /**
     * Dokładnie porównuje wartości dwóch filtrów.
     * @param filter Filtr do porównania
     * @returns Prawdę, jeśli filtry posiadają dokładnie takie same wartości, w przeciwnym wypadku fałsz
     */
    public compare(filter: Filter): boolean {
        
        return filter != null 
            && 
            this.CoordinatorsIds.sort((a,b) => a - b).join(',') 
                === filter.CoordinatorsIds.sort((a,b) => a - b).join(',')
            &&
            this.GroupsIds.sort((a,b) => a - b).join(',') 
                === filter.GroupsIds.sort((a,b) => a - b).join(',')
            &&
            this.RoomsIds.sort((a,b) => a - b).join(',') 
                === filter.RoomsIds.sort((a,b) => a - b).join(',');
    }

    public toString(): string {
        return this.toStringWithoutRooms() + `,` +
        `RoomsIds=[${this.RoomsIds.toString()}]`;
    }

    public toStringWithoutRooms(): string {
        return `CoordinatorsIds=[${this.CoordinatorsIds.toString()}],` +
        `GroupsIds=[${this.GroupsIds.toString()}]`;
    }
}