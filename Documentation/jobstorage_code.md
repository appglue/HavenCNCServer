import { Job } from '../data/Job';
import { MaterialType } from '../data/Common/MaterialLibrary';

/**
 * Metadata for a G-code file (without content)
 * Unique identifier: directory + name
 */
export interface GCodeFileMetadata {
  name: string;
  directory: string; // Full directory path where file is located
  category?: string; // Optional category/tag for organization
  description?: string;
  materialType?: MaterialType;
  estimatedTime?: string;
  lastModified: Date;
  size: number; // bytes
}

/**
 * Full G-code file with content
 */
export interface GCodeFileData extends GCodeFileMetadata {
  content: string;
}

/**
 * Metadata for a saved job (without full Job object)
 */
export interface JobMetadata {
  id: string; // Unique identifier for the job
  name: string; // Display name (can be non-unique)
  lastModified: Date;
  createdAt: Date;
  size: number; // bytes
  executionCount: number; // number of times this job was run
  lastRunDate?: Date;
  category?: string; // Optional category/tag for organization
  description?: string;
  materialType?: MaterialType;
  estimatedTime?: string;
}

/**
 * Paging parameters for list operations
 */
export interface PageRequest {
  page: number; // 0-indexed
  pageSize: number;
  sortBy?: 'name' | 'lastModified' | 'size' | 'executionCount';
  sortDirection?: 'asc' | 'desc';
}

/**
 * Paged result for list operations
 */
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

/**
 * Interface for CNC job and G-code file storage operations
 * Implemented differently for Centroid (file system/database) and Simulator (in-memory)
 */
export interface ICNCJobStorage {
  // ==================== G-CODE FILE OPERATIONS ====================

  /**
   * List G-code files in specified directories
   * @param directories Array of directory paths to search
   * @param pageRequest Paging parameters
   * @returns Paged result with file metadata (no content)
   */
  listGCodeFiles(
    directories: string[],
    pageRequest: PageRequest
  ): Promise<PagedResult<GCodeFileMetadata>>;

  /**
   * Fetch a specific G-code file with content
   * @param directory Directory path where file is located
   * @param fileName Name of the file
   * @returns Full file data with content, or null if not found
   */
  fetchGCodeFile(
    directory: string,
    fileName: string
  ): Promise<GCodeFileData | null>;

  /**
   * Store a G-code file
   * @param directory Directory path to store the file
   * @param fileData Complete file data including content
   * @returns Success status
   */
  storeGCodeFile(
    directory: string,
    fileData: GCodeFileData
  ): Promise<boolean>;

  /**
   * Delete a G-code file
   * @param directory Directory path where file is located
   * @param fileName Name of the file to delete
   * @returns Success status
   */
  deleteGCodeFile(
    directory: string,
    fileName: string
  ): Promise<boolean>;

  /**
   * Set or update the category for a G-code file
   * @param directory Directory path where file is located
   * @param fileName Name of the file
   * @param category New category to assign (or undefined to remove category)
   * @returns Success status
   */
  setGCodeFileCategory(
    directory: string,
    fileName: string,
    category: string | undefined
  ): Promise<boolean>;

  /**
   * Move a G-code file to a different category
   * This is an alias for setGCodeFileCategory for semantic clarity
   * @param directory Directory path where file is located
   * @param fileName Name of the file
   * @param newCategory Category to move the file to
   * @returns Success status
   */
  moveGCodeFileToCategory(
    directory: string,
    fileName: string,
    newCategory: string
  ): Promise<boolean>;

  // ==================== JOB OPERATIONS ====================

  /**
   * List saved jobs
   * @param pageRequest Paging parameters
   * @returns Paged result with job metadata (no full Job objects)
   */
  listJobs(pageRequest: PageRequest): Promise<PagedResult<JobMetadata>>;

  /**
   * Fetch a spId Unique identifier of the job
   * @returns Full Job object, or null if not found
   */
  fetchJob(jobId: string): Promise<Job | null>;

  /**
   * Get the last job that was run
   * This is a special fetch that returns the most recently executed job
   * @returns Full Job object of the last run job, or null if no jobs have been run
   */
  getLastJob(): Promise<Job | null>;

  /**
   * Save a job as the last job that was run
   * Updates the tracking of which job was most recently executed
   * @param job The Job object to mark as last run
   * @returns Success status
   */
  saveLastJob(job: Job): Promise<boolean>;

  /**
   * Store a job
   * @param job Complete Job object to save
   * @returns Success status
   */
  storeJob(job: Job): Promise<boolean>;

  /**
   * Delete a job
   * @param jobId Unique identifier of the job to delete
   * @returns Success status
   */
  deleteJob(jobId: string): Promise<boolean>;

  /**
   * Set or update the category for a job
   * @param jobId Unique identifier of the job
   * @param category New category to assign (or undefined to remove category)
   * @returns Success status
   */
  setJobCategory(
    jobId: string,
    category: string | undefined
  ): Promise<boolean>;

  /**
   * Move a job to a different category
   * This is an alias for setJobCategory for semantic clarity
   * @param jobId Unique identifier of the job
   * @param newCategory Category to move the job to
   * @returns Success status
   */
  moveJobToCategory(
    jobId: string,
    newCategory: string
  ): Promise<boolean>;
}
