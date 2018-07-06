import { Inject, ComponentFactoryResolver, ComponentFactory } from '@angular/core';
import { Http } from '@angular/http';
import { forEach } from '@angular/router/src/utils/collection';
import 'rxjs/add/operator/toPromise';

export class ChangesPropagator {
    private readonly viewModels = new Map<number, any>();

    constructor(
        private readonly http: Http,
        private readonly componentFactoryResolver: ComponentFactoryResolver,
        private readonly baseUrl: string) {
    }

    public async registerRequest(): Promise<void> {
        try {
            console.log('registering request');
            const request = await this.http.post(this.baseUrl + 'api/Changes/RegisterRequest', {}).toPromise();
            const response = request.json() as IResponse;

            console.log(response);

            this.processResponse(response);
            if (response.rerequest) {
                this.registerRequest();
            }
        }
        catch (error) {
            console.error(error);
        }
    }

    private processResponse(response: IResponse) {
        // assumptions:
        // the response.changes only contains properties of primitive types, that is, changes on nested components are separate entry in IResponse.changes.
        // nested component changes must be 
        for (const change of response.changes) {
            if (!(change.id in this.viewModels)) {
                console.log(`invalid json received: component with id '${change.id}' does not exist`);
                return;
            }

            const component = this.viewModels.get(change.id);

            if ('propertyName' in change) {
                this.processPropertyChange(component, change as IPropertyChange);
            } else if ('collectionName' in change) {
                const collection: any[] = component[(change as ICollectionChange).collectionName];
                if ('value' in change) {
                    this.processCollectionItemAdded(component, collection, change as ICollectionItemAdded);
                } else if ('removedItemId' in change) {
                    this.processCollectionItemRemoved(component, collection, change as ICollectionItemRemoved);
                } else if ('index1' in change) {
                    this.processCollectionItemReordered(component, collection, change as ICollectionItemsReordered);
                }
                else {
                    console.log(`invalid json received: the following change could not be interpreted`);
                    console.log(change);
                    return;
                }
            }
            else {
                console.log(`invalid json received: the following change could not be interpreted`);
                console.log(change);
                return;
            }
        }
    }

    private processPropertyChange(component: any, change: IPropertyChange) {
        component[change.propertyName] = this.toInstance(change.value);
    }
    private processCollectionItemAdded(component: any, collection: any[], change: ICollectionItemAdded) {
        const item = this.toInstance(change.item);

        //TODO: think of how to handle the case that this addition was already handled client-side. Probably some boolean from the server indicating that it knows the client-side could have already generated it.
        // same for reordering and maybe also for removal
        // another problem arises when the server pushes an addition but client-side another addition has been made. Or reordering.... 
        // So long as the client - side cannot add / remove / reorder collection items were good.otherwise it becomes complicated due to the asynchronous nature
        if (change.index !== undefined) {
            collection.splice(change.index, 0, item);
        } else {
            collection.push(item);
        }
    }
    private processCollectionItemRemoved(component: any, collection: { id: number }[], change: ICollectionItemRemoved) {
        const indexToRemove = collection.findIndex(element => element.id == change.removedItemId);
        if (indexToRemove >= 0) {
            collection.splice(indexToRemove, 1);
        }
        // otherwise the element may already have been removed by client-side code, so no error necessary
    }
    private processCollectionItemReordered(component: any, collection: any[], change: ICollectionItemsReordered) {
    }
    // converts the data on how to construct the component to the actual component; other types just pass through. 
    // on second thought: this is just the client-side model right? not the actual component. 
    // So just setting any value would do and angular will create components.Just as though you're in typescript code, there you don't have to create components yourself either.
    private toInstance(obj: admissibleTypes): any {
        return obj;
    }
}

type admissibleTypes = string | number | object;

interface IResponse {
    changes: IChange[];
    rerequest: boolean;
}
interface IChange {
    id: number;
}
interface IPropertyChange extends IChange {
    propertyName: string;
    value: admissibleTypes;
}
interface ICollectionChange extends IChange {
    collectionName: string;
}
interface ICollectionItemRemoved extends ICollectionChange {
    removedItemId: number;
}
interface ICollectionItemAdded extends ICollectionChange {
    item: admissibleTypes;
    index?: number; // if missing, append at the end of collection
}
interface ICollectionItemsReordered extends ICollectionChange {
    index1: number;
    index2: number;
}
