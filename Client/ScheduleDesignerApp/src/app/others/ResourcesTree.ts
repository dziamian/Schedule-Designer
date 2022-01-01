import { Filter } from "./Filter";

export class ResourceNode {
    children: ResourceNode[];
    item: {name: string, filter: Filter | null, icon: string};
}

export class ResourceFlatNode {
    item: {name: string, filter: Filter | null, icon: string};
    level: number;
    expandable: boolean;
}