export class Filter {
    
    constructor(
        public CoordinatorsIds: number[],
        public GroupsIds: number[],
        public RoomsIds: number[]
    ) {}

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