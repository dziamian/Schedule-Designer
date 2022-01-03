import { Filter } from "./Filter";

export class ResourceItem {
    name: string; 
    filter: Filter | null; 
    icon: string;
}

export class ResourceNode {
    children: ResourceNode[];
    item: ResourceItem;
}

export class ResourceFlatNode {
    item: ResourceItem;
    level: number;
    expandable: boolean;
    visible: boolean;
}