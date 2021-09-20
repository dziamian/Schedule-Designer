export class Coordinator {
    readonly firstName:string;
    readonly lastName:string;
    readonly title:string;

    constructor(firstName:string, lastName:string, title:string) {
        this.firstName = firstName;
        this.lastName = lastName;
        this.title = title;
    }
}