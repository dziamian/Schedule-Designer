import { Filter } from "./Filter";

export class ResourceItem {
    id: string | null;
    name: string; 
    filter: Filter | null; 
    icon: string;
    type: string | null;
    addActionType: string | null;
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