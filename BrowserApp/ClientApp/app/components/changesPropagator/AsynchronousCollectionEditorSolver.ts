import { Inject, ComponentFactoryResolver, ComponentFactory } from '@angular/core';
import { Http } from '@angular/http';
import { forEach } from '@angular/router/src/utils/collection';
import 'rxjs/add/operator/toPromise';
import { IComponent, isComponent } from './ChangesPropagator';

type Command = any;



type Item = AddedItem | RemovedItem;
class AddedItem {
    public constructor(
        public readonly collection: any[],
        public readonly item: any,
        public readonly index: number,
        public readonly command: Command) {
    }
}
class RemovedItem {
    public constructor(
        public readonly collection: any[],
        public readonly item: any,
        public readonly index: number,
        public readonly command: Command) {
    }
}
export class AsynchronousCollectionEditorSolver {

    private currentCommand: Command | null = null;
    private addedItems: AddedItem[] = [];
    private removedItems: RemovedItem[] = [];


    public onCommandStart(command: Command): void {
        if (this.currentCommand != null) {
            throw new Error('Another command is already running');
        }
        this.currentCommand = command;
    }
    public OnCommandEnd(command: Command): void {
        if (this.currentCommand !== command) {
            throw new Error(`The command could not be ended because ${this.currentCommand == null ? 'no' : 'another'} command was running`);
        }

        this.currentCommand = null;
    }
    /**
     * Notifies that a client side command added an item to a collection (as a performance improvement, to prevent waiting for a server round trip).
     */
    public onAddedClientSideToCollection(collection: any[], item: any, index: number): void {
        if (collection == null) {
            throw new Error('collection cannot be null');
        }
        if (this.currentCommand == null) {
            throw new Error('No command has been set to be currently executing');
        }

        this.addedItems.push(new AddedItem(collection, item, index, this.currentCommand));
    }
    public onAddedServerSideToCollection(collection: any[], item: any, index: number, command: Command): void {
        if (this.currentCommand != null) {
            throw new Error('A command is currently executing, whereas this method is only supposed to be called from a change received from the server');
        }

        const cachedAddition: AddedItem | null = this.findAndRemoveItem(this.removedItems, collection, command);

        if (cachedAddition != null) {
            if (isComponent(item)) {
                // SPEC: if, in the case of insertion, the collection has such a component for the current command, they're linked.
                // SPEC: if there are multiple in a collection, then the only restriction is that the clientside generates them in the same order as the server pushes them,
                // SPEC: and that there are equally many of them. 
                this.associateWithId(cachedAddition.item, item.__id);
            }
            else {
                // SPEC: insertion: we have a value and an index
                // SPEC: register a filter clientside: if from the server the exact expected item collection addition is received, ignore it. It has already been handled.
            }
            return;
        }

        const collectionModified = this.addedItems.findIndex(createdComponent => createdComponent.collection === collection) !== -1;
        if (!collectionModified) {
            collection.splice(index, 0, item);
            // the collection has not been modified by the client side since so this update must still be valid
            return;
        }

        throw new Error('Not implemented. Refetch collection from server'); // the collection has been modified such that the change from the server could not be incorporated
    }

    public onRemovedClientSideToCollection(collection: any[], item: any, index: number): void {
        if (collection == null) {
            throw new Error('collection cannot be null');
        }
        if (this.currentCommand == null) {
            throw new Error('No command has been set to be currently executing');
        }

        this.removedItems.push(new RemovedItem(collection, item, index, this.currentCommand));
    }
    public onRemovedServerSideFromCollection(collection: any[], item: any, index: number, command: Command): void {

        if (this.currentCommand != null) {
            throw new Error('A command is currently executing, whereas this method is only supposed to be called from a change received from the server');
        }

        const cachedRemoval: RemovedItem | null = this.findAndRemoveItem(this.removedItems, collection, command);

        if (cachedRemoval != null) {
            if (isComponent(item)) {
                // SPEC: it is based on id: remove the entity with that id, or if it doesnt't exist anymore, that's fine, the clientside already removed it. Simple. 
                const indexToRemove = collection.findIndex(collectionItem => collectionItem.__id === item.__id);
                if (indexToRemove) {
                    collection.splice(indexToRemove, 1);
                }
            }
            else {
                // SPEC: removal: we have a value and an index
                // SPEC: register a filter clientside: if from the server the exact expected item collection removal is received, ignore it: It has already been handled.
            }
            return;
        }

        const collectionModified = this.removedItems.findIndex(createdComponent => createdComponent.collection === collection) !== -1;
        if (!collectionModified) {
            collection.splice(index, 1);
            // the collection has not been modified by the client side since so this update must still be valid
            return;
        }

        throw new Error('Not implemented. Refetch collection from server'); // the collection has been modified such that the change from the server could not be incorporated
    }

    private findAndRemoveItem<T extends Item>(cache: Item[], collection: any[], command: Command): Item | null {
        const cacheIndex = cache.findIndex(createdComponent => createdComponent.collection === collection && createdComponent.command == command);
        const result = cacheIndex === -1 ? null : cache[cacheIndex];
        if (cacheIndex !== -1) {
            cache.splice(cacheIndex, 1);
        }
        return result;
    }

    public assertConsistentWhenNoCommandsPendingServerSide() {
        if (this.addedItems.length != 0) {
            throw new Error('The client side added some items that the server did not');
        }
        if (this.removedItems.length != 0) {
            throw new Error('The client side removed some items that the server did not');
        }
    }

    constructor(private readonly associateWithId: (component: IComponent, id: number) => void) {
    }
}
