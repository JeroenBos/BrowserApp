import { Inject, ComponentFactoryResolver, ComponentFactory } from '@angular/core';
import { Http } from '@angular/http';
import { forEach } from '@angular/router/src/utils/collection';
import 'rxjs/add/operator/toPromise';
import { AsynchronousCollectionEditorSolver } from './AsynchronousCollectionEditorSolver';
import { isComponentView } from '@angular/core/src/view/util';
import { BaseViewModel } from '../base.component';
import { CommandInstruction } from '../../commands/commands';

export interface IComponent extends BaseViewModel {
    [propertyName: string]: any;
}
export function isComponent(obj: any): obj is IComponent {
    return obj != null && obj.__id !== undefined;
}
export function assert(expr: boolean, message = "Assertion failed") {
    if (!expr) {
        throw new Error(message);
    }
}
export class ChangesPropagator {
    /** A map from component id to component. */
    private readonly components = new Map<number, IComponent>();
    public readonly collectionEditor: AsynchronousCollectionEditorSolver = new AsynchronousCollectionEditorSolver((component, id) => component.__id = id);

    constructor(
        private readonly http: Http,
        private readonly componentFactoryResolver: ComponentFactoryResolver,
        private readonly baseUrl: string,
        private readonly createInitialComponents: () => Iterable<IComponent>) {
    }

    private initializeComponents(): void {
        this.components.clear();
        for (let initialComponent of this.createInitialComponents()) {
            assert(initialComponent !== undefined, `Initial components returned an undefined component`);
            assert(initialComponent !== null, `Initial components returned a null component`);
            assert(initialComponent.__id !== null, `Initial components returned a component with non-numeric id`);
            assert(initialComponent.__id >= 0, `Initial components returned a component with invalid id ${initialComponent.__id}`);
            assert(!this.components.has(initialComponent.__id), `Initial components returned multiple components with the id ${initialComponent.__id}`);

            this.components.set(initialComponent.__id, initialComponent);
        }
        console.log(`initialized  ${this.components.size} components`);
    }

    public async open(): Promise<void> {
        this.initializeComponents();
        this.post(this.baseUrl + 'api/Changes/open');
    }
    public async registerRequest(): Promise<void> {
        this.post(this.baseUrl + 'api/Changes/RegisterRequest');
    }
    public async executeCommand(command: CommandInstruction) {
        if (command == null || command.commandId < 0 || command.viewModelId < 0 || command.eventArgs == null) {
            throw new Error("Invalid command instruction");
        }
        this.post(this.baseUrl + 'api/Changes/ExecuteCommand', command);
    }

    private async post(url: string, data?: CommandInstruction) {
        try {
            console.log(`posting '${data === undefined ? '{}' : data}' to '${url.substr(this.baseUrl.length)}'. Response: `);
            const request = await this.http.post(url, data || {}).toPromise();
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

    /**
     * If the item is a component reference, returns the associated component, otherwise returns the item itself.
     * @param item
     */
    private toInstance(item: any): any {
        if (isComponent(item)) {
            const result = this.components.get(item.__id);
            if (result === undefined) {
                throw new Error(`No viewmodel found for id '${item.__id}'`);
            }
            return result;
        }
        return item;
    }

    private processResponse(response: IResponse) {
        // assumptions:
        // the response.changes only contains properties of primitive types, that is, changes on nested components are separate entry in IResponse.changes.
        // nested component changes must be 
        for (const change of response.changes) {
            const component = this.components.get(change.id);
            if (component === undefined) {
                throw new Error(`invalid json received: component with id '${change.id}' does not exist`);
            }

            if (ChangesPropagator.IsPropertyChanged(change)) {
                this.processPropertyChange(component, change as IPropertyChange);
            } else if (ChangesPropagator.IsCollectionItemAdded(change)) {
                this.collectionEditor.onAddedServerSideToCollection(change.collectionName === undefined ? component : component[change.collectionName], this.toInstance(change.addedItemId), change.index, change.instructionId);
            } else if (ChangesPropagator.IsCollectionItemRemoved(change)) {
                this.collectionEditor.onRemovedServerSideFromCollection(change.collectionName === undefined ? component : component[change.collectionName], this.toInstance(change.removedItemId), change.index, change.instructionId);
            } else if (ChangesPropagator.IsCollectionItemReordered(change)) {
                throw new Error('reordering not implemented yet');
            } else {
                console.log(`invalid json received: the following change could not be interpreted`);
                console.log(change);
            }
        }
    }

    private static IsPropertyChanged(change: any): change is IPropertyChange {
        return 'propertyName' in change;
    }
    private static IsCollectionChanged(change: any): change is ICollectionChange {
        return 'collectionName' in change;
    }
    private static IsCollectionItemAdded(change: any): change is ICollectionItemAdded {
        return 'value' in change;
    }
    private static IsCollectionItemRemoved(change: any): change is ICollectionItemRemoved {
        return 'removedItemId' in change;
    }
    private static IsCollectionItemReordered(change: any): change is ICollectionItemsReordered {
        return 'index1' in change;
    }

    private processPropertyChange(component: IComponent, change: IPropertyChange) {
        component[change.propertyName] = this.toInstance(change.value);
    }
}

type admissibleTypes = string | number | object;

interface IResponse {
    changes: IChange[];
    rerequest: boolean;
}
interface IChange {
    /** The id of the view model containing the change. */
    id: number;
    instructionId: number;
}
interface IPropertyChange extends IChange {
    propertyName: string;
    value: admissibleTypes;
}
interface ICollectionChange extends IChange {
    /** If this is undefined, then the component represented by the id is the collection alluded to; 
     * otherwise the view model with that id contains a property with that name that holds the collection. */
    collectionName: string | undefined;
}
interface ICollectionItemRemoved extends ICollectionChange {
    removedItemId: number | undefined;
    /** The index of the item that was removed. */
    index: number;
}
interface ICollectionItemAdded extends ICollectionChange {
    item: admissibleTypes;
    /** The index at which the item was inserted. */
    index: number;
    addedItemId: number | undefined;
}
interface ICollectionItemsReordered extends ICollectionChange {
    index1: number;
    index2: number;
}